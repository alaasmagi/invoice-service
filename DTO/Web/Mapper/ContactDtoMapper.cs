using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ContactDtoMapper : IMapper<ContactDto, Contact>
{
    public ContactDto? Map(Contact? entity)
    {
        return entity == null ? null : new ContactDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone
        };
    }

    public IEnumerable<ContactDto>? Map(IEnumerable<Contact>? entities)
    {
        return entities?.Select(Map)!;
    }

    public Contact? Map(ContactDto? entity)
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

    public IEnumerable<Contact>? Map(IEnumerable<ContactDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
