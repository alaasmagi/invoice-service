using System.Security.Claims;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public abstract class UserScopedControllerBase : Controller
{
    protected Guid CurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing or is not a GUID.");
        }

        return userId;
    }

    protected void ClearServerManagedModelState(BaseEntityUserWithMetaConcurrency entity)
    {
        ModelState.Remove(nameof(entity.Id));
        ModelState.Remove(nameof(entity.UserId));
        ModelState.Remove(nameof(entity.CreatedAt));
        ModelState.Remove(nameof(entity.CreatedBy));
        ModelState.Remove(nameof(entity.UpdatedAt));
        ModelState.Remove(nameof(entity.UpdatedBy));
        ModelState.Remove(nameof(entity.ConcurrencyToken));
    }

    protected void ClearServerManagedModelState(BaseEntityUserWithConcurrency dto)
    {
        ModelState.Remove(nameof(dto.Id));
        ModelState.Remove(nameof(dto.UserId));
        ModelState.Remove(nameof(dto.ConcurrencyToken));
    }

    /// <summary>Removes navigation and collection properties, which are display-only on Web DTOs.</summary>
    protected void ClearNavigationModelState(params string[] keys)
    {
        foreach (var key in keys) ModelState.Remove(key);
    }

    /// <summary>Shows a one-time status toast on the next page. Kind: success, info, warning or danger.</summary>
    protected void Flash(string message, string kind = "success")
    {
        TempData["StatusMessage"] = message;
        TempData["StatusKind"] = kind;
    }

    protected IActionResult RedirectToLocalOr(string? returnUrl, string action, object? routeValues = null)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, routeValues);
    }

    protected void StampNew(BaseEntityUserWithMetaConcurrency entity, Guid userId)
    {
        var now = DateTime.UtcNow;
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.UserId = userId;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = userId.ToString();
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }

    protected void StampExisting(BaseEntityUserWithMetaConcurrency entity, Guid userId, BaseEntityUserWithMetaConcurrency existing)
    {
        entity.UserId = userId;
        entity.CreatedAt = existing.CreatedAt;
        entity.CreatedBy = existing.CreatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }
}
