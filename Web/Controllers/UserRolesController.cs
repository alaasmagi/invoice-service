using System.Security.Claims;
using Application.RoleManagement;
using Application.RoleManagement.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

/// <summary>
/// Delegates client-scoped user role reads and changes to the auth service after local authorization checks.
/// </summary>
[ApiController]
[Route("api/users/{userId:guid}/roles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class UserRolesController(IUserRoleManagementService userRoleManagementService) : ControllerBase
{
    /// <summary>
    /// Gets the target user's roles in this client's auth-service scope.
    /// </summary>
    /// <param name="userId">The target user ID.</param>
    /// <returns>The target user's client-scoped roles.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserRolesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        if (!TryGetRequestingUserId(out var requestingUserId))
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        var result = await userRoleManagementService.GetUserRolesAsync(userId, requestingUserId);
        return result.IsSuccess ? Ok(result.Value) : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    /// <summary>
    /// Replaces the target user's roles in this client's auth-service scope.
    /// </summary>
    /// <param name="userId">The target user ID.</param>
    /// <param name="request">The full replacement role list.</param>
    /// <returns>An empty success response when roles are replaced.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserRoles(Guid userId, [FromBody] SetUserRolesRequest request)
    {
        if (!TryGetRequestingUserId(out var requestingUserId))
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        var result = await userRoleManagementService.SetUserRolesAsync(userId, request.Roles, requestingUserId);
        return result.IsSuccess ? Ok() : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    /// <summary>
    /// Removes one role from the target user in this client's auth-service scope.
    /// </summary>
    /// <param name="userId">The target user ID.</param>
    /// <param name="roleName">The role name to remove.</param>
    /// <returns>An empty success response when the role is removed.</returns>
    [HttpDelete("{roleName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserRole(Guid userId, string roleName)
    {
        if (!TryGetRequestingUserId(out var requestingUserId))
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        var result = await userRoleManagementService.RemoveUserRoleAsync(userId, roleName, requestingUserId);
        return result.IsSuccess ? Ok() : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    private bool TryGetRequestingUserId(out Guid requestingUserId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out requestingUserId);
    }
}

/// <summary>
/// Full replacement request for a user's client-scoped roles.
/// </summary>
/// <param name="Roles">The complete role list to set for the target user.</param>
public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);
