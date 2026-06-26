using Application.Common;
using Application.RoleManagement.Dtos;

namespace Application.RoleManagement;

public interface IUserRoleManagementService
{
    Task<Result<UserRolesDto>> GetUserRolesAsync(Guid targetUserId, Guid requestingUserId);
    Task<Result<Unit>> SetUserRolesAsync(Guid targetUserId, IEnumerable<string> roles, Guid requestingUserId);
    Task<Result<Unit>> RemoveUserRoleAsync(Guid targetUserId, string roleName, Guid requestingUserId);
}
