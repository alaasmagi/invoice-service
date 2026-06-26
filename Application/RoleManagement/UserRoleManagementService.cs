using Application.Common;
using Application.RoleManagement.Dtos;
using Contracts.DataAccess;
using Domain;
using Infrastructure.AuthService;

namespace Application.RoleManagement;

public sealed class UserRoleManagementService(
    IAppRoleRepository roleRepository,
    IAuthServiceClient authServiceClient) : IUserRoleManagementService
{
    public async Task<Result<UserRolesDto>> GetUserRolesAsync(Guid targetUserId, Guid requestingUserId)
    {
        var localRoles = await roleRepository.GetAllAsync();
        var requesterRoles = await GetRequesterLocalRolesAsync(requestingUserId, localRoles);
        if (requesterRoles.Count == 0)
        {
            return Result<UserRolesDto>.Failure("Forbidden");
        }

        var targetRoles = await authServiceClient.GetUserRolesAsync(targetUserId);
        return Result<UserRolesDto>.Success(new UserRolesDto(targetUserId, targetRoles));
    }

    public async Task<Result<Unit>> SetUserRolesAsync(Guid targetUserId, IEnumerable<string> roles, Guid requestingUserId)
    {
        if (targetUserId == requestingUserId)
        {
            return Result<Unit>.Failure("CannotModifyOwnRoles");
        }

        var localRoles = await roleRepository.GetAllAsync();
        var requesterRoles = await GetRequesterLocalRolesAsync(requestingUserId, localRoles);
        if (requesterRoles.Count == 0)
        {
            return Result<Unit>.Failure("Forbidden");
        }

        var validation = ValidateRequestedRoles(roles, localRoles);
        if (validation.Error != null)
        {
            return Result<Unit>.Failure(validation.Error);
        }

        if (validation.Roles.Any(role => !CanAssignRole(role, requesterRoles)))
        {
            return Result<Unit>.Failure("InsufficientRole");
        }

        await authServiceClient.SetUserRolesAsync(targetUserId, validation.Roles.Select(role => role.Name));
        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> RemoveUserRoleAsync(Guid targetUserId, string roleName, Guid requestingUserId)
    {
        if (targetUserId == requestingUserId)
        {
            return Result<Unit>.Failure("CannotModifyOwnRoles");
        }

        var localRoles = await roleRepository.GetAllAsync();
        var role = FindLocalRole(roleName, localRoles);
        if (role == null)
        {
            return Result<Unit>.Failure($"UnknownRole:{roleName}");
        }

        await authServiceClient.RemoveUserRoleAsync(targetUserId, role.Name);
        return Result<Unit>.Success(Unit.Value);
    }

    private async Task<IReadOnlyList<AppRole>> GetRequesterLocalRolesAsync(Guid requestingUserId, IReadOnlyList<AppRole> localRoles)
    {
        var requesterRoleNames = await authServiceClient.GetUserRolesAsync(requestingUserId);
        return localRoles
            .Where(localRole => requesterRoleNames.Any(requesterRole =>
                requesterRole.Equals(localRole.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static (IReadOnlyList<AppRole> Roles, string? Error) ValidateRequestedRoles(
        IEnumerable<string> requestedRoles,
        IReadOnlyList<AppRole> localRoles)
    {
        var validatedRoles = new List<AppRole>();
        foreach (var requestedRole in requestedRoles)
        {
            var role = FindLocalRole(requestedRole, localRoles);
            if (role == null)
            {
                return ([], $"UnknownRole:{requestedRole}");
            }

            if (validatedRoles.All(existing => existing.Id != role.Id))
            {
                validatedRoles.Add(role);
            }
        }

        return (validatedRoles, null);
    }

    private static AppRole? FindLocalRole(string roleName, IReadOnlyList<AppRole> localRoles)
    {
        return localRoles.FirstOrDefault(role =>
            role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool CanAssignRole(AppRole requestedRole, IReadOnlyList<AppRole> requesterRoles)
    {
        if (requesterRoles.Any(role => role.Name.Equals(requestedRole.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return !requestedRole.IsDefault && requesterRoles.Any(role => !role.IsDefault);
    }
}
