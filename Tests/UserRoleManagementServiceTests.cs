using Application.RoleManagement;
using Domain;
using Xunit;

namespace Tests;

public sealed class UserRoleManagementServiceTests
{
    [Fact]
    public async Task GetUserRolesAsync_forbids_requester_without_local_role()
    {
        var repository = new InMemoryRoleRepository([RoleServiceTests.Role("Admin", false)]);
        var authClient = new RecordingAuthClient();
        var service = new UserRoleManagementService(repository, authClient);

        var result = await service.GetUserRolesAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task SetUserRolesAsync_rejects_self_modification()
    {
        var userId = Guid.NewGuid();
        var service = new UserRoleManagementService(
            new InMemoryRoleRepository([RoleServiceTests.Role("Admin", false)]),
            new RecordingAuthClient());

        var result = await service.SetUserRolesAsync(userId, ["Admin"], userId);

        Assert.True(result.IsFailure);
        Assert.Equal("CannotModifyOwnRoles", result.Error);
    }

    [Fact]
    public async Task SetUserRolesAsync_rejects_unknown_role_before_delegation()
    {
        var requesterId = Guid.NewGuid();
        var authClient = new RecordingAuthClient();
        authClient.UserRoles[requesterId] = ["Admin"];
        var service = new UserRoleManagementService(
            new InMemoryRoleRepository([RoleServiceTests.Role("Admin", false)]),
            authClient);

        var result = await service.SetUserRolesAsync(Guid.NewGuid(), ["Missing"], requesterId);

        Assert.True(result.IsFailure);
        Assert.Equal("UnknownRole:Missing", result.Error);
        Assert.Empty(authClient.SetRoleRequests);
    }

    [Fact]
    public async Task SetUserRolesAsync_rejects_role_above_requester_privilege()
    {
        var requesterId = Guid.NewGuid();
        var defaultRole = RoleServiceTests.Role("User", true);
        var adminRole = RoleServiceTests.Role("Admin", false);
        var authClient = new RecordingAuthClient();
        authClient.UserRoles[requesterId] = ["User"];
        var service = new UserRoleManagementService(
            new InMemoryRoleRepository([defaultRole, adminRole]),
            authClient);

        var result = await service.SetUserRolesAsync(Guid.NewGuid(), ["Admin"], requesterId);

        Assert.True(result.IsFailure);
        Assert.Equal("InsufficientRole", result.Error);
    }

    [Fact]
    public async Task RemoveUserRoleAsync_delegates_known_role()
    {
        var targetId = Guid.NewGuid();
        var service = new UserRoleManagementService(
            new InMemoryRoleRepository([RoleServiceTests.Role("Admin", false)]),
            new RecordingAuthClient());

        var result = await service.RemoveUserRoleAsync(targetId, "admin", Guid.NewGuid());

        Assert.True(result.IsSuccess);
    }
}
