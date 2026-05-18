using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AddressContactDtoMapper : IMapper<AddressContactDto, AddressContact>
{
    public AddressContactDto? Map(AddressContact? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressContactDto>? Map(IEnumerable<AddressContact>? entities)
    {
        throw new NotImplementedException();
    }

    public AddressContact? Map(AddressContactDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AddressContact>? Map(IEnumerable<AddressContactDto>? entities)
    {
        throw new NotImplementedException();
    }
}