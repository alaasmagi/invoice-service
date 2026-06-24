using System.Text.Json.Serialization;

namespace Web.IdentityHub;

public sealed record IdentityHubClaimDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value);

public sealed record IdentityHubClaimsResponse(
    [property: JsonPropertyName("claims")] List<IdentityHubClaimDto> Claims);

public sealed record IdentityHubLoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("responseType")] string ResponseType,
    [property: JsonPropertyName("redirectUri")] string RedirectUri);

public sealed record IdentityHubLoginResponse(
    [property: JsonPropertyName("authCode")] string? AuthCode,
    [property: JsonPropertyName("requiresTwoFactor")] bool RequiresTwoFactor,
    [property: JsonPropertyName("tempToken")] string? TempToken,
    [property: JsonPropertyName("requiresConsent")] bool RequiresConsent,
    [property: JsonPropertyName("consentToken")] string? ConsentToken,
    [property: JsonPropertyName("error")] string? Error);

public sealed record IdentityHubExternalChallengeRequest(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("redirectUri")] string RedirectUri,
    [property: JsonPropertyName("tenantId")] string? TenantId = null);

public sealed record IdentityHubExternalChallengeResponse(
    string? RedirectUrl,
    string? Error);

public sealed record IdentityHubTokenExchangeRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("clientSecret")] string ClientSecret);

public sealed record ApiLoginRequest(string Email, string Password, string? ReturnUrl);

public sealed record ApiExternalChallengeRequest(string Provider, string? TenantId, string? ReturnUrl);

public sealed record ApiAuthResponse(
    bool Succeeded,
    bool RequiresTwoFactor,
    bool RequiresConsent,
    string? RedirectUrl,
    string? Error);
