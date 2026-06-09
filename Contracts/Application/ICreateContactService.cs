using Domain;

namespace Contracts.Application;

public interface ICreateContactService
{
    Task<Contact> CreateAsync(Contact contact, Guid userId);
}
