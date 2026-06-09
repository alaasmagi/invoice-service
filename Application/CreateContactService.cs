using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class CreateContactService(IContactRepository contactRepository, IBaseUow uow) : ICreateContactService
{
    public async Task<Contact> CreateAsync(Contact contact, Guid userId)
    {
        contact.UserId = userId;
        if (contact.Id == Guid.Empty)
        {
            contact.Id = Guid.NewGuid();
        }

        var response = await contactRepository.CreateAsync(contact, userId);
        await uow.SaveChangesAsync();

        return response.Successful && response.Value != null
            ? response.Value
            : throw new InvalidOperationException("Contact creation failed.");
    }
}
