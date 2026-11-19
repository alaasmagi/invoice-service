using System.Security.Claims;
using System.Text.Encodings.Web;
using Contracts.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Web.Authentication;

/// <summary>
/// Authenticates API requests by the <c>X-Api-Key</c> header as the key's owner. Used only on API controllers,
/// as an owner-approved exception to the Keycloak-only API rule (see the ai-invoice-pdf-ingestion design).
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyService apiKeyService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1)
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = await apiKeyService.AuthenticateAsync(values[0]!);
        if (apiKey == null)
        {
            // One message for unknown and revoked keys, so a caller cannot tell them apart.
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()), new Claim("api_key_id", apiKey.Id.ToString())],
            SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
