using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AppRoleEntityMapper : IMapper<AppRole, AppRoleEntity>
{
    public AppRole? Map(AppRoleEntity? entity)
    {
        return entity == null ? null : new AppRole
        {
            Id = entity.Id,
            Name = entity.Name,
            IsDefault = entity.IsDefault
        };
    }

    public IEnumerable<AppRole>? Map(IEnumerable<AppRoleEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AppRoleEntity? Map(AppRole? entity)
    {
        return entity == null ? null : new AppRoleEntity
        {
            Id = entity.Id,
            Name = entity.Name,
            IsDefault = entity.IsDefault
        };
    }

    public IEnumerable<AppRoleEntity>? Map(IEnumerable<AppRole>? entities)
    {
        return entities?.Select(Map)!;
    }
}
