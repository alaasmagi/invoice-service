using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IAppUserRepository : IBaseRepository<AppUser>
{
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser> UpsertFromClaimsAsync(Guid id, string? fullName);
    Task UpdateProfileAsync(Guid id, string fullName, string? bankIban);
}
