namespace Infrastructure.AuthService;

public interface IAuthServiceClient
{
    Task SyncRolesAsync(IEnumerable<RoleSyncDefinition> roles, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
    Task SetUserRolesAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct = default);
    Task RemoveUserRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
}
