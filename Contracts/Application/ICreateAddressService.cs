using Domain;

namespace Contracts.Application;

public interface ICreateAddressService
{
    Task<Address> CreateAsync(Address address, Guid userId);
}
