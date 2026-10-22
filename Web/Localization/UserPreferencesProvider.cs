using System.Security.Claims;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Web.Localization;

/// <summary>
/// Loads a signed-in user's display preferences, cached briefly so every request doesn't hit the database.
/// The language defaults to the Keycloak-managed locale and can be overridden in the app, like format, currency and time zone.
/// </summary>
public class UserPreferencesProvider(IMemoryCache cache, IServiceScopeFactory scopeFactory)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<UserPreferences?> GetAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await cache.GetOrCreateAsync(CacheKey(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await context.AppUsers.AsNoTracking()
                .Where(e => e.Id == userId)
                .Select(e => new { e.Locale, e.Language, e.FormatCulture, e.Currency, e.TimeZone })
                .FirstOrDefaultAsync();

            // The app-level choice wins; otherwise the Keycloak locale (or its claim, before the user is projected).
            var language = LocalizationSettings.LanguageFor(row?.Language ?? row?.Locale ?? user.FindFirstValue("locale"));
            return new UserPreferences(
                language,
                LocalizationSettings.FormatCultureFor(row?.FormatCulture, language),
                LocalizationSettings.CurrencyFor(row?.Currency),
                LocalizationSettings.TimeZoneFor(row?.TimeZone).Id);
        });
    }

    public void Invalidate(Guid userId) => cache.Remove(CacheKey(userId));

    private static string CacheKey(Guid userId) => $"user-preferences:{userId}";
}
