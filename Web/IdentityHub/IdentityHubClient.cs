using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Web.IdentityHub;

public sealed class IdentityHubClient(HttpClient httpClient, IOptions<IdentityHubOptions> options) : IIdentityHubClient
{
    private readonly IdentityHubOptions _options = options.Value;

    public async Task<IdentityHubLoginResponse> LoginAsync(string email, string password, string redirectUri)
    {
        var request = new IdentityHubLoginRequest(
            email,
            password,
            _options.ClientId,
            "cookie",
            redirectUri);

        var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return new IdentityHubLoginResponse(null, false, null, false, null, "IdentityHubLoginFailed");
        }

        return await response.Content.ReadFromJsonAsync<IdentityHubLoginResponse>()
               ?? new IdentityHubLoginResponse(null, false, null, false, null, "EmptyIdentityHubResponse");
    }

    public async Task<IdentityHubExternalChallengeResponse> ExternalChallengeAsync(string provider, string redirectUri, string? tenantId)
    {
        var request = new IdentityHubExternalChallengeRequest(provider, _options.ClientId, redirectUri, tenantId);
        var response = await httpClient.PostAsJsonAsync("api/auth/external/challenge", request);

        if (response.Headers.Location is { } location)
        {
            return new IdentityHubExternalChallengeResponse(location.ToString(), null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new IdentityHubExternalChallengeResponse(null, "IdentityHubExternalChallengeFailed");
        }

        var body = await response.Content.ReadFromJsonAsync<IdentityHubExternalChallengeBody>();
        return new IdentityHubExternalChallengeResponse(body?.RedirectUrl ?? body?.Url, body?.Error);
    }

    public async Task<IdentityHubClaimsResponse> ExchangeCodeAsync(string code)
    {
        var request = new IdentityHubTokenExchangeRequest(code, _options.ClientId, _options.ClientSecret);
        var response = await httpClient.PostAsJsonAsync("api/auth/external/token/exchange", request);

        if (!response.IsSuccessStatusCode)
        {
            throw new IdentityHubException("Could not exchange identity-hub authentication code.");
        }

        return await response.Content.ReadFromJsonAsync<IdentityHubClaimsResponse>()
               ?? throw new IdentityHubException("Identity-hub returned an empty claims response.");
    }

    private sealed class IdentityHubExternalChallengeBody
    {
        public string? RedirectUrl { get; set; }
        public string? Url { get; set; }
        public string? Error { get; set; }
    }
}
