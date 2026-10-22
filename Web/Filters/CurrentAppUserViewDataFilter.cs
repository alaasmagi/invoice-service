using System.Security.Claims;
using Contracts.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Filters;

public class CurrentAppUserViewDataFilter(IAppUserRepository appUserRepository) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller &&
            context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var (email, fullName) = await CurrentAppUser(context);
            controller.ViewData["CurrentAppUserEmail"] = email;
            controller.ViewData["CurrentAppUserFullName"] = fullName;
            controller.ViewData["CurrentAppUserFirstName"] = FirstName(fullName) ?? email;
        }

        await next();
    }

    private async Task<(string Email, string? FullName)> CurrentAppUser(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        var fallbackEmail = FirstClaimValue(user, ClaimTypes.Email, "email") ?? user.Identity?.Name ?? "Account";
        var fallbackName = FirstClaimValue(user, "name")
                           ?? FirstClaimValue(user, ClaimTypes.GivenName, "given_name");

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return (fallbackEmail, fallbackName);
        }

        var appUser = await appUserRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
        return (
            string.IsNullOrWhiteSpace(appUser?.Email) ? fallbackEmail : appUser.Email,
            string.IsNullOrWhiteSpace(appUser?.FullName) ? fallbackName : appUser.FullName);
    }

    /// <summary>"Aleksander Laasmägi" → "Aleksander"; null when there is no usable name.</summary>
    private static string? FirstName(string? fullName) =>
        fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

    private static string? FirstClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        return claimTypes
            .Select(claimType => user.FindFirstValue(claimType))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
