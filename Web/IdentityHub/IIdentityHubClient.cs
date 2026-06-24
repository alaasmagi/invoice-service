namespace Web.IdentityHub;

public interface IIdentityHubClient
{
    Task<IdentityHubLoginResponse> LoginAsync(string email, string password, string redirectUri);
    Task<IdentityHubExternalChallengeResponse> ExternalChallengeAsync(string provider, string redirectUri, string? tenantId);
    Task<IdentityHubClaimsResponse> ExchangeCodeAsync(string code);
}
