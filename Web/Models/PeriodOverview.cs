using DTO.DataAccess.Web.DTO;
using Web.Helpers;

namespace Web.Models;

/// <summary>Where a billing month stands: its invoices, generated statements and what still needs doing.</summary>
public class PeriodOverview
{
    public BillingPeriod Period { get; init; }

    public IReadOnlyList<InvoiceDto> Invoices { get; init; } = [];

    public decimal InvoiceTotal => Invoices.Sum(e => e.TotalSum);

    /// <summary>Invoices whose address had no active resident on the invoice date; generation skips them.</summary>
    public IReadOnlyList<InvoiceDto> InvoicesWithoutResidents { get; init; } = [];

    /// <summary>Address + service pairs billed in the previous month but not in this one.</summary>
    public IReadOnlyList<(AddressDto Address, ServiceDto Service)> MissingInvoices { get; init; } = [];

    public IReadOnlyList<MonthlyStatementDto> Statements { get; init; } = [];

    public decimal StatementTotal => Statements.Sum(e => e.TotalSum);

    public int UnsentCount => Statements.Count(e => StatementRules.IsUnsent(e.Status));

    public decimal UnsentTotal => Statements.Where(e => StatementRules.IsUnsent(e.Status)).Sum(e => e.TotalSum);

    public int SentCount => Statements.Count(e => e.Status == Domain.EMonthlyStatementStatus.Sent);

    public int FailedCount => Statements.Count(e => e.Status == Domain.EMonthlyStatementStatus.Failed);

    /// <summary>Invoices added, changed or moved since the statements were last generated.</summary>
    public int PendingInvoiceCount { get; init; }

    public int AllocatableInvoiceCount => Invoices.Count - InvoicesWithoutResidents.Count;

    public bool NeedsGeneration => AllocatableInvoiceCount > 0 && (Statements.Count == 0 || PendingInvoiceCount > 0);

    public bool AllSent => Statements.Count > 0 && SentCount == Statements.Count && !NeedsGeneration;
}

public static class StatementRules
{
    public static bool IsUnsent(Domain.EMonthlyStatementStatus status) =>
        status is Domain.EMonthlyStatementStatus.Draft
            or Domain.EMonthlyStatementStatus.ReadyToSend
            or Domain.EMonthlyStatementStatus.PartiallySent
            or Domain.EMonthlyStatementStatus.Failed;
}
