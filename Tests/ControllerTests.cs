using System.Security.Claims;
using Application.Common;
using Application.RoleManagement;
using Application.RoleManagement.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Controllers;
using Xunit;

namespace Tests;

public sealed class ControllerTests
{
    [Fact]
    public async Task GetUserRoles_returns_unauthorized_when_name_identifier_is_missing()
    {
        var controller = new UserRolesController(new StubUserRoleManagementService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetUserRoles(Guid.NewGuid());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_maps_forbidden_service_result_to_403()
    {
        var service = new StubUserRoleManagementService { GetResult = Result<UserRolesDto>.Failure("Forbidden") };
        var controller = new UserRolesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                        "Bearer"))
                }
            }
        };

        var result = await controller.GetUserRoles(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    private sealed class StubUserRoleManagementService : IUserRoleManagementService
    {
        public Result<UserRolesDto> GetResult { get; init; } =
            Result<UserRolesDto>.Success(new UserRolesDto(Guid.NewGuid(), []));

        public Task<Result<UserRolesDto>> GetUserRolesAsync(Guid targetUserId, Guid requestingUserId)
        {
            return Task.FromResult(GetResult);
        }

        public Task<Result<Unit>> SetUserRolesAsync(Guid targetUserId, IEnumerable<string> roles, Guid requestingUserId)
        {
            return Task.FromResult(Result<Unit>.Success(Unit.Value));
        }

        public Task<Result<Unit>> RemoveUserRoleAsync(Guid targetUserId, string roleName, Guid requestingUserId)
        {
            return Task.FromResult(Result<Unit>.Success(Unit.Value));
        }
    }
}
