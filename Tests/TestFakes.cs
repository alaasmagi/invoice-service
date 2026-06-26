using Contracts.DataAccess;
using Domain;
using Infrastructure.AuthService;

namespace Tests;

internal sealed class InMemoryRoleRepository(IEnumerable<AppRole>? seed = null) : IAppRoleRepository
{
    private readonly List<AppRole> _roles = seed?.Select(Clone).ToList() ?? [];

    public Task<IReadOnlyList<AppRole>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<AppRole>>(_roles.Select(Clone).ToList());
    }

    public Task<AppRole?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(_roles.FirstOrDefault(role => role.Id == id) is { } role ? Clone(role) : null);
    }

    public Task<AppRole> CreateAsync(AppRole role)
    {
        var clone = Clone(role);
        _roles.Add(clone);
        return Task.FromResult(Clone(clone));
    }

    public Task<AppRole?> UpdateAsync(AppRole role)
    {
        var existing = _roles.FirstOrDefault(item => item.Id == role.Id);
        if (existing == null)
        {
            return Task.FromResult<AppRole?>(null);
        }

        existing.Name = role.Name;
        existing.IsDefault = role.IsDefault;
        return Task.FromResult<AppRole?>(Clone(existing));
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var role = _roles.FirstOrDefault(item => item.Id == id);
        if (role == null)
        {
            return Task.FromResult(false);
        }

        _roles.Remove(role);
        return Task.FromResult(true);
    }

    private static AppRole Clone(AppRole role)
    {
        return new AppRole
        {
            Id = role.Id,
            Name = role.Name,
            IsDefault = role.IsDefault
        };
    }
}

internal sealed class RecordingAuthClient : IAuthServiceClient
{
    public Dictionary<Guid, IReadOnlyList<string>> UserRoles { get; } = [];
    public List<IReadOnlyList<RoleSyncDefinition>> SyncedRoles { get; } = [];
    public List<(Guid UserId, IReadOnlyList<string> Roles)> SetRoleRequests { get; } = [];
    public List<(Guid UserId, string RoleName)> RemoveRoleRequests { get; } = [];
    public Exception? SyncException { get; init; }

    public Task SyncRolesAsync(IEnumerable<RoleSyncDefinition> roles, CancellationToken ct = default)
    {
        if (SyncException != null)
        {
            throw SyncException;
        }

        SyncedRoles.Add(roles.ToList());
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return Task.FromResult(UserRoles.GetValueOrDefault(userId, []));
    }

    public Task SetUserRolesAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        SetRoleRequests.Add((userId, roles.ToList()));
        return Task.CompletedTask;
    }

    public Task RemoveUserRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        RemoveRoleRequests.Add((userId, roleName));
        return Task.CompletedTask;
    }
}
