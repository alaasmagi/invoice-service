using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class ApiKeyService(
    IApiKeyRepository apiKeyRepository,
    IBaseUow uow,
    TimeProvider timeProvider) : IApiKeyService
{
    public const string KeyPrefix = "isk_";
    public const int MaxNameLength = 128;
    private const int RandomBytes = 32;
    private const int DisplayPrefixLength = 12;

    public async Task<IMethodResponse<ApiKeyCreated>> CreateAsync(string name, Guid userId)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > MaxNameLength)
        {
            return MethodResponse<ApiKeyCreated>.Failure(
                new Error(ApiKeyErrors.InvalidName, $"The name must be 1 to {MaxNameLength} characters."));
        }

        var plainKey = KeyPrefix + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RandomBytes));
        var created = await apiKeyRepository.CreateAsync(new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyHash = Hash(plainKey),
            KeyPrefix = plainKey[..DisplayPrefixLength]
        }, userId);
        if (!created.Successful || created.Value == null)
        {
            throw new InvalidOperationException("API key creation failed.");
        }

        await uow.SaveChangesAsync();
        return MethodResponse<ApiKeyCreated>.Success(new ApiKeyCreated { ApiKey = created.Value, PlainKey = plainKey });
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllAsync(Guid userId)
    {
        var apiKeys = await apiKeyRepository.GetAllAsync(userId);
        return (apiKeys.Value ?? []).OrderByDescending(apiKey => apiKey.CreatedAt).ToList();
    }

    public async Task<IMethodResponse<ApiKey>> RevokeAsync(Guid apiKeyId, Guid userId)
    {
        var found = await apiKeyRepository.GetByIdAsync(apiKeyId, userId);
        if (!found.Successful || found.Value == null)
        {
            return MethodResponse<ApiKey>.Failure(new Error(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound));
        }

        if (found.Value.IsActive)
        {
            found.Value.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
            await UpdateAsync(found.Value, userId);
        }

        return MethodResponse<ApiKey>.Success(found.Value);
    }

    public async Task<ApiKey?> AuthenticateAsync(string plainKey)
    {
        if (string.IsNullOrWhiteSpace(plainKey) || !plainKey.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var apiKey = await apiKeyRepository.FindByHashAsync(Hash(plainKey));
        if (apiKey is not { IsActive: true })
        {
            return null;
        }

        apiKey.LastUsedAt = timeProvider.GetUtcNow().UtcDateTime;
        await UpdateAsync(apiKey, apiKey.UserId);
        return apiKey;
    }

    // The key has 256 bits of entropy, so a fast hash is enough; a slow hash would only add latency per request.
    public static string Hash(string plainKey)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plainKey)));
    }

    private async Task UpdateAsync(ApiKey apiKey, Guid userId)
    {
        var updated = await apiKeyRepository.UpdateAsync(apiKey.Id, apiKey, actor: userId);
        if (!updated.Successful)
        {
            throw new InvalidOperationException("API key update failed.");
        }

        await uow.SaveChangesAsync();
    }
}
