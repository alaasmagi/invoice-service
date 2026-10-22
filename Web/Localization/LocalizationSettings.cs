using System.Globalization;

namespace Web.Localization;

/// <summary>The languages, formats, currencies and time zones a user can pick from.</summary>
public static class LocalizationSettings
{
    public const string DefaultLanguage = "en";
    public const string DefaultCurrency = "EUR";
    public const string DefaultTimeZone = "Europe/Tallinn";

    /// <summary>UI languages with a translation (English is the source text).</summary>
    public static readonly IReadOnlyList<string> Languages = ["en", "et"];

    /// <summary>Cultures offered for date and number formatting.</summary>
    public static readonly IReadOnlyList<string> FormatCultures =
        ["et-EE", "en-GB", "en-US", "en-IE", "fi-FI", "lv-LV", "lt-LT", "sv-SE", "de-DE", "ru-RU"];

    public static readonly IReadOnlyList<string> Currencies = ["EUR", "USD", "GBP", "SEK", "NOK", "DKK", "PLN", "CHF"];

    /// <summary>Maps a Keycloak locale ("et", "et-EE", "en_US", …) to a supported UI language.</summary>
    public static string LanguageFor(string? locale)
    {
        var code = (locale ?? string.Empty).Trim().Replace('_', '-').Split('-')[0].ToLowerInvariant();
        return Languages.Contains(code) ? code : DefaultLanguage;
    }

    /// <summary>The user's own format choice, or the usual format for their language.</summary>
    public static string FormatCultureFor(string? formatCulture, string language) =>
        !string.IsNullOrWhiteSpace(formatCulture) && FormatCultures.Contains(formatCulture)
            ? formatCulture
            : language == "et" ? "et-EE" : "en-GB";

    public static string CurrencyFor(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) && Currencies.Contains(currency) ? currency : DefaultCurrency;

    public static TimeZoneInfo TimeZoneFor(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        return TimeZoneInfo.TryFindSystemTimeZoneById(DefaultTimeZone, out var fallback) ? fallback : TimeZoneInfo.Utc;
    }

    /// <summary>The symbol for an ISO currency code in a given culture, e.g. EUR → "€", SEK → "kr".</summary>
    public static string CurrencySymbol(string currency)
    {
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region;
            try { region = new RegionInfo(culture.Name); }
            catch (ArgumentException) { continue; }
            if (region.ISOCurrencySymbol == currency) return region.CurrencySymbol;
        }
        return currency;
    }
}
