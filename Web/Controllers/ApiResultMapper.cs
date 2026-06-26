using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

internal static class ApiResultMapper
{
    public static IActionResult ToErrorResult(ControllerBase controller, string? error)
    {
        return error switch
        {
            "Forbidden" or "InsufficientRole" or "CannotModifyOwnRoles" => controller.Forbid(),
            "NotFound" or "UserNotInClient" => controller.NotFound(new { error }),
            "Unauthorized" => controller.Unauthorized(new { error }),
            "CannotAssignAdminRole" => controller.BadRequest(new { error }),
            { } value when value.StartsWith("UnknownRole:", StringComparison.OrdinalIgnoreCase) =>
                controller.BadRequest(new { error }),
            _ => controller.BadRequest(new { error = error ?? "BadRequest" })
        };
    }
}
