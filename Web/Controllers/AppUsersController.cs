using System.Security.Claims;
using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Localization;
using Web.Models;

namespace Web.Controllers;

public class AppUsersController(AppDbContext context, UserPreferencesProvider preferencesProvider) : UserScopedControllerBase
{
    public async Task<IActionResult> Edit()
    {
        var userId = CurrentUserId();
        var entity = await context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, HttpContext.RequestAborted);

        return View(ToViewModel(userId, entity));
    }

    public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
    {
        ViewData["AccountUrl"] = KeycloakAccountUrl();
        base.OnActionExecuted(context);
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
        ModelState.Remove(nameof(model.Locale));
        if (!string.IsNullOrEmpty(model.FormatCulture) && !LocalizationSettings.FormatCultures.Contains(model.FormatCulture))
            ModelState.AddModelError(nameof(model.FormatCulture), T["Choose a format from the list."]);
        if (!LocalizationSettings.Currencies.Contains(model.Currency))
            ModelState.AddModelError(nameof(model.Currency), T["Choose a currency from the list."]);
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(model.TimeZone, out _))
            ModelState.AddModelError(nameof(model.TimeZone), T["Choose a time zone from the list."]);

        if (!ModelState.IsValid)
        {
            model.Locale = (await context.AppUsers.AsNoTracking().Where(user => user.Id == userId)
                .Select(user => user.Locale).FirstOrDefaultAsync(HttpContext.RequestAborted)) ?? model.Locale;
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
        // The language (Locale) is owned by Keycloak and synced from identity events, so it isn't written here.
        entity.BankIban = string.IsNullOrWhiteSpace(model.BankIban) ? null : model.BankIban.Trim();
        entity.FormatCulture = string.IsNullOrEmpty(model.FormatCulture) ? null : model.FormatCulture;
        entity.Currency = model.Currency;
        entity.TimeZone = model.TimeZone;

        await context.SaveChangesAsync(HttpContext.RequestAborted);
        preferencesProvider.Invalidate(userId);
        Flash(T["Your settings were saved."]);

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
            BankIban = entity?.BankIban,
            FormatCulture = entity?.FormatCulture,
            Currency = LocalizationSettings.CurrencyFor(entity?.Currency),
            TimeZone = LocalizationSettings.TimeZoneFor(entity?.TimeZone).Id
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

    /// <summary>Keycloak's account console, where the user changes their language.</summary>
    private static string? KeycloakAccountUrl()
    {
        var authority = Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY");
        return string.IsNullOrWhiteSpace(authority) ? null : authority.TrimEnd('/') + "/account";
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
