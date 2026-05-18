using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ContactDtoMapper : IMapper<ContactDto, Contact>
{
    public ContactDto? Map(Contact? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactDto>? Map(IEnumerable<Contact>? entities)
    {
        throw new NotImplementedException();
    }

    public Contact? Map(ContactDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Contact>? Map(IEnumerable<ContactDto>? entities)
    {
        throw new NotImplementedException();
    }
}