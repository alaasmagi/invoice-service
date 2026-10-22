using System.Globalization;
using Domain;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Web.Localization;

namespace Web.Helpers;

/// <summary>
/// Formats amounts, dates and times for the current user: numbers and dates follow their format culture,
/// amounts carry their currency, timestamps are shown in their time zone. Injected into views as <c>Fmt</c>.
/// </summary>
public class Formatter(IHttpContextAccessor httpContextAccessor, IStringLocalizer<SharedResource> t)
{
    private NumberFormatInfo? _moneyFormat;

    public UserPreferences Preferences =>
        httpContextAccessor.HttpContext?.Items[UserPreferencesCultureProvider.ItemKey] as UserPreferences
        ?? UserPreferences.Default with { FormatCulture = CultureInfo.CurrentCulture.Name, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName };

    public static CultureInfo Culture => CultureInfo.CurrentCulture;

    public string CurrencySymbol => MoneyFormat.CurrencySymbol;

    private NumberFormatInfo MoneyFormat
    {
        get
        {
            if (_moneyFormat != null) return _moneyFormat;
            var format = (NumberFormatInfo)Culture.NumberFormat.Clone();
            format.CurrencySymbol = LocalizationSettings.CurrencySymbol(Preferences.Currency);
            return _moneyFormat = format;
        }
    }

    public string Money(decimal value) => value.ToString("C2", MoneyFormat);

    public string Number(decimal value, int decimals = 0) => value.ToString("N" + decimals, Culture);

    /// <summary>A percentage given as 0–100, e.g. 12.4 → "12%".</summary>
    public string Percent(decimal value) => (value / 100m).ToString("P0", Culture);

    public string Date(DateOnly value) => value.ToString("d", Culture);

    public string Date(DateOnly? value, string empty = "—") => value.HasValue ? Date(value.Value) : empty;

    /// <summary>A UTC timestamp shown in the user's time zone.</summary>
    public string Timestamp(DateTime? value, string? empty = null)
    {
        if (!value.HasValue) return empty ?? "—";
        var utc = DateTime.SpecifyKind(value.Value, value.Value.Kind == DateTimeKind.Local ? DateTimeKind.Local : DateTimeKind.Utc).ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Preferences.TimeZone).ToString("g", Culture);
    }

    public string Range(DateOnly? start, DateOnly? end) =>
        start == null && end == null ? "—" : $"{Date(start, "…")} – {Date(end, "…")}";

    public string MonthName(int month) => Capitalize(CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(month));

    public string StatusLabel(EMonthlyStatementStatus status) => status switch
    {
        EMonthlyStatementStatus.Draft => t["Draft"],
        EMonthlyStatementStatus.ReadyToSend => t["Ready to send"],
        EMonthlyStatementStatus.PartiallySent => t["Partially sent"],
        EMonthlyStatementStatus.Sent => t["Sent"],
        EMonthlyStatementStatus.Failed => t["Failed"],
        _ => status.ToString()
    };

    public IEnumerable<SelectListItem> StatusOptions() =>
        Enum.GetValues<EMonthlyStatementStatus>().Select(s => new SelectListItem(StatusLabel(s), s.ToString()));

    public IEnumerable<SelectListItem> MonthOptions() =>
        Enumerable.Range(1, 12).Select(m => new SelectListItem(MonthName(m), m.ToString(CultureInfo.InvariantCulture)));

    /// <summary>A value for &lt;input type="number"&gt;, which always expects the invariant format.</summary>
    public static string InputNumber(decimal value) => value == 0 ? "" : value.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>A CSS-safe number (decimal point, no grouping), for inline styles.</summary>
    public static string Css(decimal value) => Math.Round(value, 4).ToString(CultureInfo.InvariantCulture);

    internal static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], CultureInfo.CurrentUICulture) + text[1..];
}
