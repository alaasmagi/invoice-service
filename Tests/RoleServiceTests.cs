using System.Net;
using Application.Roles;
using Application.Roles.Requests;
using Contracts.DataAccess;
using Domain;
using Infrastructure.AuthService;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_duplicate_role_names_case_insensitively()
    {
        var repository = new InMemoryRoleRepository([Role("Admin", false)]);
        var service = new RoleService(repository, new RecordingAuthClient(), NullLogger<RoleService>.Instance);

        var result = await service.CreateAsync(new CreateRoleRequest { Name = "admin", IsDefault = false });

        Assert.True(result.IsFailure);
        Assert.Equal("DuplicateRoleName", result.Error);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_clears_existing_default_and_syncs_complete_role_list()
    {
        var repository = new InMemoryRoleRepository([Role("User", true)]);
        var authClient = new RecordingAuthClient();
        var service = new RoleService(repository, authClient, NullLogger<RoleService>.Instance);

        var result = await service.CreateAsync(new CreateRoleRequest { Name = "Admin", IsDefault = true });

        Assert.True(result.IsSuccess);
        var roles = await repository.GetAllAsync();
        Assert.False(roles.Single(role => role.Name == "User").IsDefault);
        Assert.True(roles.Single(role => role.Name == "Admin").IsDefault);
        Assert.Equal(["Admin", "User"], authClient.SyncedRoles.Single().Select(role => role.Name).Order());
    }

    [Fact]
    public async Task Sync_failure_after_create_is_non_fatal()
    {
        var repository = new InMemoryRoleRepository();
        var service = new RoleService(
            repository,
            new RecordingAuthClient { SyncException = new AuthServiceException(HttpStatusCode.BadGateway, "DownstreamUnavailable") },
            NullLogger<RoleService>.Instance);

        var result = await service.CreateAsync(new CreateRoleRequest { Name = "Admin", IsDefault = false });

        Assert.True(result.IsSuccess);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_does_not_sync_roles()
    {
        var role = Role("Admin", false);
        var repository = new InMemoryRoleRepository([role]);
        var authClient = new RecordingAuthClient();
        var service = new RoleService(repository, authClient, NullLogger<RoleService>.Instance);

        var result = await service.DeleteAsync(role.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(authClient.SyncedRoles);
    }

    internal static AppRole Role(string name, bool isDefault)
    {
        return new AppRole { Id = Guid.NewGuid(), Name = name, IsDefault = isDefault };
    }
}
