using System.Net;
using System.Text;
using Infrastructure.AuthService;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests;

public sealed class AuthServiceClientTests
{
    [Fact]
    public async Task SyncRolesAsync_sends_credentials_and_roles_payload()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"syncedRoles":["Admin"],"defaultRole":"User"}""")
        });
        var client = CreateClient(handler);

        await client.SyncRolesAsync([new RoleSyncDefinition("Admin", false)]);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/api/client/roles/sync", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Equal("11111111-1111-1111-1111-111111111111", handler.Requests[0].Headers.GetValues("X-Client-Id").Single());
        Assert.Equal("client-secret", handler.Requests[0].Headers.GetValues("X-Client-Secret").Single());
        Assert.Contains("\"name\":\"Admin\"", handler.Bodies[0], StringComparison.Ordinal);
        Assert.Contains("\"isDefault\":false", handler.Bodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUserRolesAsync_returns_empty_list_on_not_found()
    {
        var client = CreateClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var roles = await client.GetUserRolesAsync(Guid.NewGuid());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task RemoveUserRoleAsync_url_encodes_role_name()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);
        var userId = Guid.NewGuid();

        await client.RemoveUserRoleAsync(userId, "Admin/Finance");

        Assert.Equal($"/api/client/users/{userId}/roles/Admin%2FFinance", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Non_success_response_throws_typed_exception_with_error_code()
    {
        var client = CreateClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"InvalidRole"}""", Encoding.UTF8, "application/json")
        }));

        var exception = await Assert.ThrowsAsync<AuthServiceException>(() =>
            client.SetUserRolesAsync(Guid.NewGuid(), ["Admin"]));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("InvalidRole", exception.ErrorCode);
    }

    private static AuthServiceClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth.example.test/")
        };
        var options = Options.Create(new AuthServiceOptions
        {
            BaseUrl = "https://auth.example.test",
            ClientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ClientSecret = "client-secret"
        });

        return new AuthServiceClient(httpClient, options, NullLogger<AuthServiceClient>.Instance);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return responseFactory(request);
        }
    }
}
