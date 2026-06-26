using Application.Common;
using Application.Roles.Requests;
using Application.Roles.Validators;
using Contracts.DataAccess;
using Domain;
using FluentValidation;
using Infrastructure.AuthService;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

public sealed class RoleService(
    IAppRoleRepository roleRepository,
    IAuthServiceClient authServiceClient,
    ILogger<RoleService> logger) : IRoleService
{
    private static readonly CreateRoleRequestValidator CreateValidator = new();
    private static readonly UpdateRoleRequestValidator UpdateValidator = new();

    public async Task<Result<IReadOnlyList<AppRole>>> GetAllAsync()
    {
        return Result<IReadOnlyList<AppRole>>.Success(await roleRepository.GetAllAsync());
    }

    public async Task<Result<AppRole>> CreateAsync(CreateRoleRequest request)
    {
        var validation = await CreateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result<AppRole>.Failure(validation.Errors[0].ErrorMessage);
        }

        var name = request.Name.Trim();
        var allRoles = await roleRepository.GetAllAsync();
        if (allRoles.Any(role => role.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<AppRole>.Failure("DuplicateRoleName");
        }

        if (request.IsDefault)
        {
            await ClearOtherDefaultRolesAsync(Guid.Empty, allRoles);
        }

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDefault = request.IsDefault
        };

        var created = await roleRepository.CreateAsync(role);
        await TrySyncWithAuthServiceAsync();

        return Result<AppRole>.Success(created);
    }

    public async Task<Result<AppRole>> UpdateAsync(UpdateRoleRequest request)
    {
        var validation = await UpdateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result<AppRole>.Failure(validation.Errors[0].ErrorMessage);
        }

        var role = await roleRepository.GetByIdAsync(request.Id);
        if (role == null)
        {
            return Result<AppRole>.Failure("NotFound");
        }

        var name = request.Name.Trim();
        var allRoles = await roleRepository.GetAllAsync();
        if (allRoles.Any(existing => existing.Id != request.Id &&
                                     existing.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<AppRole>.Failure("DuplicateRoleName");
        }

        if (request.IsDefault && !role.IsDefault)
        {
            await ClearOtherDefaultRolesAsync(role.Id, allRoles);
        }

        role.Name = name;
        role.IsDefault = request.IsDefault;

        var updated = await roleRepository.UpdateAsync(role);
        if (updated == null)
        {
            return Result<AppRole>.Failure("NotFound");
        }

        await TrySyncWithAuthServiceAsync();

        return Result<AppRole>.Success(updated);
    }

    public async Task<Result<Unit>> DeleteAsync(Guid id)
    {
        var deleted = await roleRepository.DeleteAsync(id);
        return deleted
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure("NotFound");
    }

    private async Task ClearOtherDefaultRolesAsync(Guid roleIdToKeep, IReadOnlyList<AppRole> allRoles)
    {
        foreach (var role in allRoles.Where(role => role.Id != roleIdToKeep && role.IsDefault))
        {
            role.IsDefault = false;
            await roleRepository.UpdateAsync(role);
        }
    }

    private async Task TrySyncWithAuthServiceAsync()
    {
        var allRoles = await roleRepository.GetAllAsync();
        var definitions = allRoles.Select(role => new RoleSyncDefinition(role.Name, role.IsDefault));

        try
        {
            await authServiceClient.SyncRolesAsync(definitions);
            logger.LogInformation("Role sync with auth service completed.");
        }
        catch (AuthServiceException ex)
        {
            logger.LogError(
                ex,
                "Role sync with auth service failed with status {StatusCode}. Local roles are unchanged. Auth service will be reconciled on next sync.",
                ex.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during role sync with auth service.");
        }
    }
}
