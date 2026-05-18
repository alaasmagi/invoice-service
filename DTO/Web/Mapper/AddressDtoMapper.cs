using Base.Contracts.DTO;
using Base.Domain;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AddressDtoMapper : IMapper<AddressDto, Address>
{
    public AddressDto? Map(Address? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressDto>? Map(IEnumerable<Address>? entities)
    {
        throw new NotImplementedException();
    }

    public Address? Map(AddressDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Address>? Map(IEnumerable<AddressDto>? entities)
    {
        throw new NotImplementedException();
    }
}