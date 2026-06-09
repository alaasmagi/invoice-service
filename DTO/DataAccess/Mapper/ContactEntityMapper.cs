using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ContactEntityMapper : IMapper<Contact, ContactEntity>
{
    public Contact? Map(ContactEntity? entity)
    {
        return entity == null ? null : new Contact
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone
        };
    }

    public IEnumerable<Contact>? Map(IEnumerable<ContactEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public ContactEntity? Map(Contact? entity)
    {
        return entity == null ? null : new ContactEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone
        };
    }

    public IEnumerable<ContactEntity>? Map(IEnumerable<Contact>? entities)
    {
        return entities?.Select(Map)!;
    }
}
