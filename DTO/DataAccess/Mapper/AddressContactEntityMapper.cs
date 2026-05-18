using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AddressContactEntityMapper : IMapper<AddressContact, AddressContactEntity>
{
    public AddressContact? Map(AddressContactEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressContact>? Map(IEnumerable<AddressContactEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public AddressContactEntity? Map(AddressContact? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressContactEntity>? Map(IEnumerable<AddressContact>? entities)
    {
        throw new NotImplementedException();
    }
}