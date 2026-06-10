using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IAddressRepository : IBaseRepository<Address>
{
    Task<IReadOnlyList<Address>> GetAllForUserAsync(Guid userId);
}
