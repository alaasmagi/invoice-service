using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AppUserEntityMapper : IMapper<AppUser, AppUserEntity>
{
    public AppUser? Map(AppUserEntity? entity)
    {
        return entity == null ? null : new AppUser
        {
            Id = entity.Id,
            Fullname = entity.Fullname,
            BankIban = entity.BankIban
        };
    }

    public IEnumerable<AppUser>? Map(IEnumerable<AppUserEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AppUserEntity? Map(AppUser? entity)
    {
        return entity == null ? null : new AppUserEntity
        {
            Id = entity.Id,
            Fullname = entity.Fullname,
            BankIban = entity.BankIban
        };
    }

    public IEnumerable<AppUserEntity>? Map(IEnumerable<AppUser>? entities)
    {
        return entities?.Select(Map)!;
    }
}
