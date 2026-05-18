using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AddressEntityMapper : IMapper<Address, AddressEntity>
{
    public Address? Map(AddressEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Address>? Map(IEnumerable<AddressEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public AddressEntity? Map(Address? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressEntity>? Map(IEnumerable<Address>? entities)
    {
        throw new NotImplementedException();
    }
}