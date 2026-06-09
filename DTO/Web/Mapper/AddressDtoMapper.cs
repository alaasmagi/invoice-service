using Base.Contracts.DTO;
using Base.Domain;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AddressDtoMapper : IMapper<AddressDto, Address>
{
    public AddressDto? Map(Address? entity)
    {
        return entity == null ? null : new AddressDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            FullAddress = entity.FullAddress
        };
    }

    public IEnumerable<AddressDto>? Map(IEnumerable<Address>? entities)
    {
        return entities?.Select(Map)!;
    }

    public Address? Map(AddressDto? entity)
    {
        return entity == null ? null : new Address
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            FullAddress = entity.FullAddress
        };
    }

    public IEnumerable<Address>? Map(IEnumerable<AddressDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
