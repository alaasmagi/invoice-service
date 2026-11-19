using Base.Contracts.DTO;
using Domain;

namespace Contracts.Application;

/// <summary>Creates, lists, revokes and checks users' API keys. Keys are stored only as hashes.</summary>
public interface IApiKeyService
{
    Task<IMethodResponse<ApiKeyCreated>> CreateAsync(string name, Guid userId);

    Task<IReadOnlyList<ApiKey>> GetAllAsync(Guid userId);

    Task<IMethodResponse<ApiKey>> RevokeAsync(Guid apiKeyId, Guid userId);

    /// <summary>The active key matching <paramref name="plainKey"/>, with its last use recorded; null otherwise.</summary>
    Task<ApiKey?> AuthenticateAsync(string plainKey);
}

public class ApiKeyCreated
{
    public ApiKey ApiKey { get; init; } = default!;

    /// <summary>The full key. It is returned only here and cannot be shown again.</summary>
    public string PlainKey { get; init; } = default!;
}

public static class ApiKeyErrors
{
    public const string InvalidName = "API_KEY_INVALID_NAME";
}
