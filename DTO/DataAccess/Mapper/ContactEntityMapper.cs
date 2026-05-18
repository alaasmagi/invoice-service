using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ContactEntityMapper : IMapper<Contact, ContactEntity>
{
    public Contact? Map(ContactEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Contact>? Map(IEnumerable<ContactEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public ContactEntity? Map(Contact? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactEntity>? Map(IEnumerable<Contact>? entities)
    {
        throw new NotImplementedException();
    }
}