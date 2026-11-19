using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

// The key hash never leaves the application layer, so it is not part of the DTO.
public class ApiKeyDtoMapper : IMapper<ApiKeyDto, ApiKey>
{
    public ApiKeyDto? Map(ApiKey? entity)
    {
        return entity == null ? null : new ApiKeyDto
        {
            Id = entity.Id,
            Name = entity.Name,
            KeyPrefix = entity.KeyPrefix,
            CreatedAt = entity.CreatedAt,
            LastUsedAt = entity.LastUsedAt,
            RevokedAt = entity.RevokedAt
        };
    }

    public IEnumerable<ApiKeyDto>? Map(IEnumerable<ApiKey>? entities)
    {
        return entities?.Select(Map)!;
    }

    public ApiKey? Map(ApiKeyDto? entity)
    {
        return entity == null ? null : new ApiKey
        {
            Id = entity.Id,
            Name = entity.Name,
            KeyPrefix = entity.KeyPrefix,
            CreatedAt = entity.CreatedAt,
            LastUsedAt = entity.LastUsedAt,
            RevokedAt = entity.RevokedAt
        };
    }

    public IEnumerable<ApiKey>? Map(IEnumerable<ApiKeyDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
