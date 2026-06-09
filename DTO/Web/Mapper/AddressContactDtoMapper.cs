using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AddressContactDtoMapper : IMapper<AddressContactDto, AddressContact>
{
    public AddressContactDto? Map(AddressContact? entity)
    {
        return entity == null ? null : new AddressContactDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            ContactId = entity.ContactId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    public IEnumerable<AddressContactDto>? Map(IEnumerable<AddressContact>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AddressContact? Map(AddressContactDto? entity)
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

    public IEnumerable<AddressContact>? Map(IEnumerable<AddressContactDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
