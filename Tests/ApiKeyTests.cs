using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application;
using Base.Contracts.DataAccess;
using Base.DTO;
using Base.Keycloak.Authentication;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.Web.DTO;
using DTO.DataAccess.Web.Mapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Web.Authentication;
using Web.Configuration;

using Web.Controllers.Api;
using Xunit;

namespace Tests;

public class ApiKeyTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IApiKeyRepository> _apiKeys = new();
    private readonly Mock<IBaseUow> _uow = new();
    private ApiKey? _stored;

    public ApiKeyTests()
    {
        _apiKeys
            .Setup(r => r.CreateAsync(It.IsAny<ApiKey>(), Owner))
            .ReturnsAsync((ApiKey apiKey, Guid _) =>
            {
                apiKey.UserId = Owner;
                _stored = apiKey;
                return MethodResponse<ApiKey>.Success(apiKey);
            });
        _apiKeys
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ApiKey>(), null, It.IsAny<Guid>()))
            .ReturnsAsync((Guid _, ApiKey apiKey, string? _, Guid _) =>
            {
                _stored = apiKey;
                return MethodResponse<ApiKey>.Success(apiKey);
            });
    }

    private ApiKeyService CreateSut() => new(_apiKeys.Object, _uow.Object, new FixedTimeProvider(Now));

    [Fact]
    public async Task Create_ReturnsPrefixedRandomKeyOnce_AndStoresOnlyItsHash()
    {
        var created = (await CreateSut().CreateAsync("  Gmail forwarder  ", Owner)).Value!;

        Assert.StartsWith("isk_", created.PlainKey);
        Assert.True(created.PlainKey.Length >= 4 + 43); // 32 bytes base64url-encoded
        Assert.Equal("Gmail forwarder", _stored!.Name);
        Assert.Equal(ApiKeyService.Hash(created.PlainKey), _stored.KeyHash);
        Assert.Equal(created.PlainKey[..12], _stored.KeyPrefix);
        Assert.DoesNotContain(created.PlainKey, new[] { _stored.KeyHash, _stored.KeyPrefix, _stored.Name });
        Assert.Equal(Owner, created.ApiKey.UserId);
    }

    [Fact]
    public async Task Create_GeneratesDifferentKeys()
    {
        var first = (await CreateSut().CreateAsync("a", Owner)).Value!.PlainKey;
        var second = (await CreateSut().CreateAsync("b", Owner)).Value!.PlainKey;

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsInvalidName(string name)
    {
        var result = await CreateSut().CreateAsync(name, Owner);

        Assert.Equal(ApiKeyErrors.InvalidName, result.Error!.Code);
        _apiKeys.Verify(r => r.CreateAsync(It.IsAny<ApiKey>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Authenticate_ValidKey_ReturnsOwnerAndRecordsUse()
    {
        const string plainKey = "isk_valid";
        var apiKey = new ApiKey { Id = Guid.NewGuid(), UserId = Owner, KeyHash = ApiKeyService.Hash(plainKey) };
        _apiKeys.Setup(r => r.FindByHashAsync(apiKey.KeyHash)).ReturnsAsync(apiKey);

        var authenticated = await CreateSut().AuthenticateAsync(plainKey);

        Assert.Equal(Owner, authenticated!.UserId);
        Assert.Equal(Now.UtcDateTime, _stored!.LastUsedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("isk_unknown")]
    [InlineData("not-a-key")]
    [InlineData("isk_revoked")]
    public async Task Authenticate_MissingUnknownOrRevokedKey_ReturnsNull(string plainKey)
    {
        var revoked = new ApiKey { UserId = Owner, KeyHash = ApiKeyService.Hash("isk_revoked"), RevokedAt = Now.UtcDateTime };
        _apiKeys.Setup(r => r.FindByHashAsync(revoked.KeyHash)).ReturnsAsync(revoked);

        Assert.Null(await CreateSut().AuthenticateAsync(plainKey));
        _apiKeys.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ApiKey>(), null, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_OwnKey_SetsRevokedAt()
    {
        var apiKey = new ApiKey { Id = Guid.NewGuid(), UserId = Owner };
        _apiKeys.Setup(r => r.GetByIdAsync(apiKey.Id, Owner)).ReturnsAsync(MethodResponse<ApiKey>.Success(apiKey));

        var result = await CreateSut().RevokeAsync(apiKey.Id, Owner);

        Assert.False(result.Value!.IsActive);
        Assert.Equal(Now.UtcDateTime, _stored!.RevokedAt);
    }

    [Fact]
    public async Task Revoke_AnotherUsersKey_IsNotFound()
    {
        var otherUser = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        _apiKeys.Setup(r => r.GetByIdAsync(keyId, otherUser))
            .ReturnsAsync(MethodResponse<ApiKey>.Failure(new Error(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound)));

        var result = await CreateSut().RevokeAsync(keyId, otherUser);

        Assert.Equal(ErrorDefaults.Codes.NotFound, result.Error!.Code);
        _uow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handler_WithoutHeader_DoesNotAuthenticate()
    {
        var result = await AuthenticateAsync(null, new Mock<IApiKeyService>().Object);

        Assert.True(result.None);
    }

    [Fact]
    public async Task Handler_WithInvalidKey_Fails_WithoutSayingWhy()
    {
        var service = new Mock<IApiKeyService>();

        var result = await AuthenticateAsync("isk_bad", service.Object);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API key.", result.Failure!.Message);
    }

    [Fact]
    public async Task Handler_WithValidKey_AuthenticatesAsOwner()
    {
        var service = new Mock<IApiKeyService>();
        service.Setup(s => s.AuthenticateAsync("isk_good")).ReturnsAsync(new ApiKey { Id = Guid.NewGuid(), UserId = Owner });

        var result = await AuthenticateAsync("isk_good", service.Object);

        Assert.True(result.Succeeded);
        Assert.Equal(Owner.ToString(), result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(ApiKeyAuthenticationHandler.SchemeName, result.Ticket!.AuthenticationScheme);
    }

    [Fact]
    public void ApiKey_IsOnlyUsedByApiControllers_WhileMvcKeepsTheCookieScheme()
    {
        var services = new ServiceCollection();
        services.AddKeycloakOidc(new KeycloakOptions { Authority = "https://id.example/realms/r", ClientId = "c", ClientSecret = "s" })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
        var authentication = services.BuildServiceProvider().GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.NotEqual(ApiKeyAuthenticationHandler.SchemeName, authentication.DefaultScheme);
        Assert.NotEqual(ApiKeyAuthenticationHandler.SchemeName, authentication.DefaultAuthenticateScheme);

        var mvcAuthorize = typeof(Web.Controllers.UserScopedControllerBase).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        var apiAuthorize = typeof(InvoiceDocumentsApiController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Null(mvcAuthorize.AuthenticationSchemes);
        Assert.Equal(ApiKeyAuthenticationHandler.SchemeName, apiAuthorize.AuthenticationSchemes);
    }

    [Fact]
    public void ApiController_UsesTheNamedRateLimitPolicy()
    {
        var attribute = typeof(InvoiceDocumentsApiController).GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
            .Cast<EnableRateLimitingAttribute>().Single();

        Assert.Equal(ApiRateLimiting.PolicyName, attribute.PolicyName);
        Assert.Equal(30, ApiRateLimiting.PermitsPerMinute);
    }

    [Fact]
    public void RateLimit_IsPartitionedPerUser_WithIpFallback()
    {
        var other = Guid.NewGuid();

        Assert.Equal($"user:{Owner}", ApiRateLimiting.PartitionKey(ContextFor(Owner)));
        Assert.NotEqual(ApiRateLimiting.PartitionKey(ContextFor(Owner)), ApiRateLimiting.PartitionKey(ContextFor(other)));
        Assert.Equal("ip:203.0.113.7", ApiRateLimiting.PartitionKey(ContextFor(null)));
    }

    [Fact]
    public async Task Endpoint_NewPdf_Is202_AndDuplicateIs200()
    {
        var document = new InvoiceDocument { Id = Guid.NewGuid(), FileName = "a.pdf", Status = EInvoiceDocumentStatus.Processing };
        var service = new Mock<IInvoiceDocumentService>();
        service.SetupSequence(s => s.UploadAsync(It.IsAny<InvoiceDocumentUploadRequest>(), Owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MethodResponse<InvoiceDocumentUploadResult>.Success(new InvoiceDocumentUploadResult { Document = document }))
            .ReturnsAsync(MethodResponse<InvoiceDocumentUploadResult>.Success(new InvoiceDocumentUploadResult { Document = document, IsDuplicate = true }));
        var controller = Controller(service.Object);

        var created = Assert.IsType<AcceptedResult>(await controller.Upload(File(), CancellationToken.None));
        var duplicate = Assert.IsType<OkObjectResult>(await controller.Upload(File(), CancellationToken.None));

        var dto = Assert.IsType<InvoiceDocumentDto>(created.Value);
        Assert.Equal(document.Id, dto.Id);
        Assert.Equal("Processing", dto.Status);
        Assert.Equal("a.pdf", dto.FileName);
        Assert.IsType<InvoiceDocumentDto>(duplicate.Value);
    }

    [Fact]
    public async Task Endpoint_InvalidOrMissingFile_Is400()
    {
        var service = new Mock<IInvoiceDocumentService>();
        service.Setup(s => s.UploadAsync(It.IsAny<InvoiceDocumentUploadRequest>(), Owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MethodResponse<InvoiceDocumentUploadResult>.Failure(new Error(InvoiceDocumentErrors.NotPdf, "The file is not a PDF.")));
        var controller = Controller(service.Object);

        var invalid = Assert.IsType<BadRequestObjectResult>(await controller.Upload(File(), CancellationToken.None));
        var missing = Assert.IsType<BadRequestObjectResult>(await controller.Upload(null, CancellationToken.None));

        Assert.Equal(InvoiceDocumentErrors.NotPdf, Assert.IsType<ProblemDetails>(invalid.Value).Extensions["code"]);
        Assert.Equal("INVOICE_PDF_MISSING", Assert.IsType<ProblemDetails>(missing.Value).Extensions["code"]);
    }

    private static InvoiceDocumentsApiController Controller(IInvoiceDocumentService service) => new(service, new InvoiceDocumentDtoMapper())
    {
        ControllerContext = new ControllerContext { HttpContext = ContextFor(Owner) }
    };

    private static IFormFile File()
    {
        var bytes = "%PDF-1.7"u8.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "a.pdf");
    }

    private static DefaultHttpContext ContextFor(Guid? userId)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        if (userId != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString()!)], ApiKeyAuthenticationHandler.SchemeName));
        }

        return context;
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(string? headerValue, IApiKeyService service)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(o => o.Get(ApiKeyAuthenticationHandler.SchemeName)).Returns(new AuthenticationSchemeOptions());
        var handler = new ApiKeyAuthenticationHandler(options.Object, NullLoggerFactory.Instance, UrlEncoder.Default, service);
        var context = new DefaultHttpContext();
        if (headerValue != null)
        {
            context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = headerValue;
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationHandler.SchemeName, null, typeof(ApiKeyAuthenticationHandler)), context);
        return await handler.AuthenticateAsync();
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
