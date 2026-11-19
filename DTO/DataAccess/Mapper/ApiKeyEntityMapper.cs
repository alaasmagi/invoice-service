using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ApiKeyEntityMapper : IMapper<ApiKey, ApiKeyEntity>
{
    public ApiKey? Map(ApiKeyEntity? entity)
    {
        return entity == null ? null : new ApiKey
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            KeyHash = entity.KeyHash,
            KeyPrefix = entity.KeyPrefix,
            CreatedAt = entity.CreatedAt,
            LastUsedAt = entity.LastUsedAt,
            RevokedAt = entity.RevokedAt
        };
    }

    public IEnumerable<ApiKey>? Map(IEnumerable<ApiKeyEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    // CreatedAt is metadata the repository sets on create, so it is not copied back.
    public ApiKeyEntity? Map(ApiKey? entity)
    {
        return entity == null ? null : new ApiKeyEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            KeyHash = entity.KeyHash,
            KeyPrefix = entity.KeyPrefix,
            LastUsedAt = entity.LastUsedAt,
            RevokedAt = entity.RevokedAt
        };
    }

    public IEnumerable<ApiKeyEntity>? Map(IEnumerable<ApiKey>? entities)
    {
        return entities?.Select(Map)!;
    }
}
