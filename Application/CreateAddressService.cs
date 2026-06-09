using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class CreateAddressService(IAddressRepository addressRepository, IBaseUow uow) : ICreateAddressService
{
    public async Task<Address> CreateAsync(Address address, Guid userId)
    {
        address.UserId = userId;
        if (address.Id == Guid.Empty)
        {
            address.Id = Guid.NewGuid();
        }

        var response = await addressRepository.CreateAsync(address, userId);
        await uow.SaveChangesAsync();

        return response.Successful && response.Value != null
            ? response.Value
            : throw new InvalidOperationException("Address creation failed.");
    }
}
