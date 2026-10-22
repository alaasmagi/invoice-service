using System.Globalization;
using Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Helpers;

/// <summary>Consistent display formatting for amounts, dates and periods across all views.</summary>
public static class Fmt
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Money(decimal value) => value.ToString("N2", Culture) + " €";

    public static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", Culture);

    public static string Date(DateOnly? value, string empty = "—") => value.HasValue ? Date(value.Value) : empty;

    public static string Timestamp(DateTime? value, string empty = "—") =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", Culture) + " UTC" : empty;

    public static string Range(DateOnly? start, DateOnly? end) =>
        start == null && end == null ? "—" : $"{Date(start, "…")} – {Date(end, "…")}";

    public static string MonthName(int month) => Culture.DateTimeFormat.GetMonthName(month);

    public static string StatusLabel(EMonthlyStatementStatus status) => status switch
    {
        EMonthlyStatementStatus.ReadyToSend => "Ready to send",
        EMonthlyStatementStatus.PartiallySent => "Partially sent",
        _ => status.ToString()
    };

    public static IEnumerable<SelectListItem> StatusOptions() =>
        Enum.GetValues<EMonthlyStatementStatus>().Select(s => new SelectListItem(StatusLabel(s), s.ToString()));
}
