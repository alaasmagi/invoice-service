using System.Security.Claims;
using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Controllers;

public sealed class AppUsersController(AppDbContext context) : UserScopedControllerBase
{
    public async Task<IActionResult> Edit()
    {
        var userId = CurrentUserId();
        var entity = await context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, HttpContext.RequestAborted);

        return View(ToViewModel(userId, entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AppUserEditViewModel model)
    {
        var userId = CurrentUserId();
        if (model.Id != userId)
        {
            return NotFound();
        }

        model.Email = IdentityEmail();
        model.Username = IdentityUsername();
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.Username));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await context.AppUsers
            .FirstOrDefaultAsync(user => user.Id == userId, HttpContext.RequestAborted);

        if (entity == null)
        {
            entity = new AppUserEntity
            {
                Id = userId,
                Email = model.Email,
                Username = model.Username
            };
            StampNew(entity, userId);
            context.AppUsers.Add(entity);
        }
        else
        {
            StampExisting(entity, userId);
        }

        entity.FullName = model.FullName.Trim();
        entity.Locale = NormalizeLocale(model.Locale);
        entity.BankIban = string.IsNullOrWhiteSpace(model.BankIban) ? null : model.BankIban.Trim();

        await context.SaveChangesAsync(HttpContext.RequestAborted);
        Flash("Your details were saved.");

        return RedirectToAction(nameof(Edit));
    }

    private AppUserEditViewModel ToViewModel(Guid userId, AppUserEntity? entity)
    {
        return new AppUserEditViewModel
        {
            Id = userId,
            Email = string.IsNullOrWhiteSpace(entity?.Email) ? IdentityEmail() : entity.Email,
            Username = string.IsNullOrWhiteSpace(entity?.Username) ? IdentityUsername() : entity.Username,
            FullName = string.IsNullOrWhiteSpace(entity?.FullName) ? IdentityFullName() : entity.FullName,
            Locale = string.IsNullOrWhiteSpace(entity?.Locale) ? "et" : entity.Locale,
            BankIban = entity?.BankIban
        };
    }

    private string IdentityEmail()
    {
        return FirstClaimValue(ClaimTypes.Email, "email") ?? User.Identity?.Name ?? string.Empty;
    }

    private string IdentityUsername()
    {
        return FirstClaimValue("preferred_username", "username", ClaimTypes.Name) ?? User.Identity?.Name ?? string.Empty;
    }

    private string IdentityFullName()
    {
        var fullName = FirstClaimValue("name", ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var firstName = FirstClaimValue(ClaimTypes.GivenName, "given_name");
        var lastName = FirstClaimValue(ClaimTypes.Surname, "family_name");
        return string.Join(' ', new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private string? FirstClaimValue(params string[] claimTypes)
    {
        return claimTypes
            .Select(claimType => User.FindFirstValue(claimType))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string NormalizeLocale(string locale)
    {
        return string.IsNullOrWhiteSpace(locale) ? "et" : locale.Trim();
    }

    private static void StampNew(AppUserEntity entity, Guid userId)
    {
        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = userId.ToString();
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }

    private static void StampExisting(AppUserEntity entity, Guid userId)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }
}
