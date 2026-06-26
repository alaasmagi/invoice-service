using Application.Common;
using Application.Roles.Requests;
using Domain;

namespace Application.Roles;

public interface IRoleService
{
    Task<Result<IReadOnlyList<AppRole>>> GetAllAsync();
    Task<Result<AppRole>> CreateAsync(CreateRoleRequest request);
    Task<Result<AppRole>> UpdateAsync(UpdateRoleRequest request);
    Task<Result<Unit>> DeleteAsync(Guid id);
}
