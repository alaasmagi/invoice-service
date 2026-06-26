using Application.Roles;
using Application.Roles.Requests;
using Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

/// <summary>
/// Manages this service's local role records and synchronizes successful changes to the auth service.
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Gets all local roles.
    /// </summary>
    /// <returns>The list of roles from this service's local database.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppRole>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await roleService.GetAllAsync();
        return result.IsSuccess ? Ok(result.Value) : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    /// <summary>
    /// Creates a local role and synchronizes the complete role list to the auth service.
    /// </summary>
    /// <param name="request">The role creation request.</param>
    /// <returns>The created role.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AppRole), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await roleService.CreateAsync(request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRoles), new { id = result.Value!.Id }, result.Value)
            : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    /// <summary>
    /// Updates a local role and synchronizes the complete role list to the auth service.
    /// </summary>
    /// <param name="id">The local role ID.</param>
    /// <param name="request">The role update request.</param>
    /// <returns>The updated role.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AppRole), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await roleService.UpdateAsync(request with { Id = id });
        return result.IsSuccess ? Ok(result.Value) : ApiResultMapper.ToErrorResult(this, result.Error);
    }

    /// <summary>
    /// Deletes a local role without synchronizing deletion to the auth service.
    /// </summary>
    /// <param name="id">The local role ID.</param>
    /// <returns>An empty success response when the role is deleted.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var result = await roleService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : ApiResultMapper.ToErrorResult(this, result.Error);
    }
}
