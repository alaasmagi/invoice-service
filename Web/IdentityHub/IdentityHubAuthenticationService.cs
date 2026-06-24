using System.Security.Claims;
using Contracts.DataAccess;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.IdentityHub;

public sealed class IdentityHubAuthenticationService(IAppUserRepository appUserRepository)
{
    public async Task<Guid> SignInAsync(HttpContext httpContext, IdentityHubClaimsResponse response)
    {
        var claims = response.Claims
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

        var rawUserId = claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new IdentityHubException("Identity-hub did not return a valid user id claim.");
        }

        var fullName = FirstClaimValue(claims, ClaimTypes.Name, "name", "Fullname", "fullname");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            var email = FirstClaimValue(claims, ClaimTypes.Email, ClaimTypes.Name, "email");
            if (!string.IsNullOrWhiteSpace(email) && claims.All(claim => claim.Type != ClaimTypes.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, email));
            }
        }

        await appUserRepository.UpsertFromClaimsAsync(userId, fullName);

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return userId;
    }

    private static string? FirstClaimValue(IEnumerable<Claim> claims, params string[] types)
    {
        return claims.FirstOrDefault(claim => types.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))?.Value;
    }
}
