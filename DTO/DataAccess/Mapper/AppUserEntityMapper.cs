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
            Email = entity.Email,
            Username = entity.Username,
            FullName = entity.FullName,
            Locale = entity.Locale,
            BankIban = entity.BankIban,
            FormatCulture = entity.FormatCulture,
            Currency = entity.Currency,
            TimeZone = entity.TimeZone
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
            Email = entity.Email,
            Username = entity.Username,
            FullName = entity.FullName,
            Locale = entity.Locale,
            BankIban = entity.BankIban,
            FormatCulture = entity.FormatCulture,
            Currency = entity.Currency,
            TimeZone = entity.TimeZone
        };
    }

    public IEnumerable<AppUserEntity>? Map(IEnumerable<AppUser>? entities)
    {
        return entities?.Select(Map)!;
    }
}
