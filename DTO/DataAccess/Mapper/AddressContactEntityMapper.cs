using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AddressContactEntityMapper : IMapper<AddressContact, AddressContactEntity>
{
    public AddressContact? Map(AddressContactEntity? entity)
    {
        return entity == null ? null : new AddressContact
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            ContactId = entity.ContactId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    public IEnumerable<AddressContact>? Map(IEnumerable<AddressContactEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AddressContactEntity? Map(AddressContact? entity)
    {
        return entity == null ? null : new AddressContactEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            ContactId = entity.ContactId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    public IEnumerable<AddressContactEntity>? Map(IEnumerable<AddressContact>? entities)
    {
        return entities?.Select(Map)!;
    }
}
