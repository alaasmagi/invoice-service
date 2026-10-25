using System.Globalization;
using Contracts.Application;
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

    /// <summary>A share percentage given as 0–100, keeping decimals only when needed: 60 → "60%", 66.67 → "66.67%".</summary>
    public string SharePercent(decimal value) => (value / 100m).ToString(value % 1 == 0 ? "P0" : "P2", Culture);

    public string MethodLabel(EAllocationMethod method) => method switch
    {
        EAllocationMethod.Equal => t["Equal"],
        EAllocationMethod.Percentage => t["Percentage"],
        EAllocationMethod.Fixed => t["Fixed amount"],
        EAllocationMethod.Manual => t["Manual"],
        _ => method.ToString()
    };

    /// <summary>How a statement line's amount was calculated, e.g. "÷ 3", "60%", "Fixed", "Remainder ÷ 2", "Manual".</summary>
    public string SplitLabel(EAllocationMethod method, decimal? shareValue, int participants) => method switch
    {
        EAllocationMethod.Percentage when shareValue.HasValue => SharePercent(shareValue.Value),
        EAllocationMethod.Fixed when shareValue.HasValue => t["Fixed"],
        EAllocationMethod.Fixed => t["Remainder ÷ {0}", participants],
        EAllocationMethod.Manual => t["Manual"],
        _ => $"÷ {participants}"
    };

    public string AllocationWarningText(AllocationWarning warning) => warning.Kind switch
    {
        EAllocationWarningKind.NoActiveResidents => t["Nobody lived at the address on the invoice date, so it was not billed."],
        EAllocationWarningKind.UnlistedResident => t["{0} lives here but is not part of the split rule, so they pay nothing for it.", warning.ContactName ?? t["A resident"]],
        EAllocationWarningKind.NoListedResidentActive => t["Nobody in the split rule lived here on the invoice date, so it was split equally instead."],
        EAllocationWarningKind.FixedExceedsTotal => t["The fixed amounts ({0}) are more than the invoice total, so it was not billed.", Money(warning.Amount ?? 0)],
        EAllocationWarningKind.RemainderWithoutPayer => t["{0} is left after the fixed amounts and nobody else lives here to pay it, so it was not billed.", Money(warning.Amount ?? 0)],
        EAllocationWarningKind.ManualSumMismatch => t["The manual split differs from the invoice total by {0}, so it was not billed.", Money(warning.Amount ?? 0)],
        _ => warning.Kind.ToString()
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
