using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AddressEntityMapper : IMapper<Address, AddressEntity>
{
    public Address? Map(AddressEntity? entity)
    {
        return entity == null ? null : new Address
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            FullAddress = entity.FullAddress
        };
    }

    public IEnumerable<Address>? Map(IEnumerable<AddressEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AddressEntity? Map(Address? entity)
    {
        return entity == null ? null : new AddressEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            FullAddress = entity.FullAddress
        };
    }

    public IEnumerable<AddressEntity>? Map(IEnumerable<Address>? entities)
    {
        return entities?.Select(Map)!;
    }
}
