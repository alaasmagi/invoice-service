namespace Contracts.DataAccess;

/// <summary>
/// Applies one identity-hub user event to the local user table together with the idempotency guard,
/// in a single transaction. Returns false when the envelope id was already processed (duplicate
/// delivery) so the caller can ack silently.
/// </summary>
public interface IIdentityUserProjection
{
    Task<bool> TryProjectAsync(IdentityUserProjectionInput input, CancellationToken ct = default);
}

public sealed record IdentityUserProjectionInput(
    string EnvelopeId,
    string Source,
    string Action,
    Guid UserId,
    string? Email,
    string? Username,
    string? FullName,
    string? Locale);
