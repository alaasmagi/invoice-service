using System.Security.Claims;
using Contracts.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Filters;

public sealed class CurrentAppUserViewDataFilter(IAppUserRepository appUserRepository) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller &&
            context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            controller.ViewData["CurrentAppUserEmail"] = await CurrentAppUserEmail(context);
        }

        await next();
    }

    private async Task<string> CurrentAppUserEmail(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        var fallbackEmail = FirstClaimValue(user, ClaimTypes.Email, "email") ?? user.Identity?.Name ?? "Account";
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return fallbackEmail;
        }

        var appUser = await appUserRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
        return string.IsNullOrWhiteSpace(appUser?.Email) ? fallbackEmail : appUser.Email;
    }

    private static string? FirstClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        return claimTypes
            .Select(claimType => user.FindFirstValue(claimType))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
