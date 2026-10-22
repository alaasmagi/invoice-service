using Microsoft.AspNetCore.Localization;

namespace Web.Localization;

/// <summary>Sets the request culture (formats) and UI culture (language) from the signed-in user's preferences.</summary>
public sealed class UserPreferencesCultureProvider : RequestCultureProvider
{
    public const string ItemKey = "UserPreferences";

    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices.GetRequiredService<UserPreferencesProvider>();
        var preferences = await provider.GetAsync(httpContext.User);
        if (preferences == null) return null;

        httpContext.Items[ItemKey] = preferences;
        return new ProviderCultureResult(preferences.FormatCulture, preferences.Language);
    }
}
