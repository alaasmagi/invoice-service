using Domain;

namespace Contracts.DataAccess;

public interface IAppRoleRepository
{
    Task<IReadOnlyList<AppRole>> GetAllAsync();
    Task<AppRole?> GetByIdAsync(Guid id);
    Task<AppRole> CreateAsync(AppRole role);
    Task<AppRole?> UpdateAsync(AppRole role);
    Task<bool> DeleteAsync(Guid id);
}
