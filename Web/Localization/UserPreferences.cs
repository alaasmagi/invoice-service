using System.Globalization;

namespace Web.Localization;

/// <summary>Resolved display preferences for the current request.</summary>
public sealed record UserPreferences(string Language, string FormatCulture, string Currency, string TimeZoneId)
{
    public static readonly UserPreferences Default = new(
        LocalizationSettings.DefaultLanguage,
        LocalizationSettings.FormatCultureFor(null, LocalizationSettings.DefaultLanguage),
        LocalizationSettings.DefaultCurrency,
        LocalizationSettings.DefaultTimeZone);

    public CultureInfo Culture => CultureInfo.GetCultureInfo(FormatCulture);

    public CultureInfo UiCulture => CultureInfo.GetCultureInfo(Language);

    public TimeZoneInfo TimeZone => LocalizationSettings.TimeZoneFor(TimeZoneId);
}
