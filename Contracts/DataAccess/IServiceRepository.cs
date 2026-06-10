using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IServiceRepository : IBaseRepository<Service>
{
    Task<IReadOnlyList<Service>> GetAllForUserAsync(Guid userId);
}
