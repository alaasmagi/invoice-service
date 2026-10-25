using Contracts.Application;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Helpers;

namespace Web.Models;

public class DeleteViewModel<T>
{
    public T Item { get; init; } = default!;

    /// <summary>Reasons the delete cannot go ahead; the delete button is hidden when any exist.</summary>
    public IReadOnlyList<string> Blockers { get; init; } = [];

    /// <summary>Related records that will be removed along with the item.</summary>
    public IReadOnlyList<string> Consequences { get; init; } = [];

    public DeleteNotice Notice(string noun) => new(noun, Blockers, Consequences);
}

public record DeleteNotice(string Noun, IReadOnlyList<string> Blockers, IReadOnlyList<string> Consequences);

public class InvoicesIndexViewModel
{
    public string? Period { get; init; }
    public Guid? AddressId { get; init; }
    public Guid? ServiceId { get; init; }
    public IReadOnlyList<BillingPeriod> PeriodOptions { get; init; } = [];
    public IEnumerable<SelectListItem> AddressOptions { get; init; } = [];
    public IEnumerable<SelectListItem> ServiceOptions { get; init; } = [];
    public IReadOnlyList<InvoiceDto> Invoices { get; init; } = [];
    public bool IsFiltered => Period != null || AddressId != null || ServiceId != null;
}

public class InvoiceFormViewModel
{
    public bool HasAddresses { get; init; }
    public bool HasServices { get; init; }

    /// <summary>Statements that already include this invoice (edit only).</summary>
    public IReadOnlyList<MonthlyStatementDto> IncludedInStatements { get; init; } = [];
}

public class ShareRow
{
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = default!;
    public decimal Amount { get; set; }
    /// <summary>How the amount was calculated, e.g. "÷ 3" or "60%".</summary>
    public string SplitLabel { get; set; } = "";
}

public class InvoiceDetailsViewModel
{
    public InvoiceDto Invoice { get; init; } = default!;

    /// <summary>How the next statement generation splits this invoice (manual split, split rule or equal split).</summary>
    public IReadOnlyList<ShareRow> Split { get; init; } = [];
    public Domain.EAllocationMethod SplitMethod { get; init; }
    public Guid? SplitRuleId { get; init; }
    public IReadOnlyList<AllocationWarning> SplitWarnings { get; init; } = [];

    public IReadOnlyList<MonthlyStatementLineDto> StatementLines { get; init; } = [];
}

public class AddressDetailsViewModel
{
    public AddressDto Address { get; init; } = default!;
    public IReadOnlyList<AddressContactDto> Residents { get; init; } = [];
    public IReadOnlyList<InvoiceDto> RecentInvoices { get; init; } = [];
    public int InvoiceCount { get; init; }
    public IReadOnlyList<AllocationRuleDto> SplitRules { get; init; } = [];
    /// <summary>Current residents left out of a current Percentage or Fixed rule, by rule id.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<string>> UnlistedResidents { get; init; } = new Dictionary<Guid, IReadOnlyList<string>>();
}

public class ContactDetailsViewModel
{
    public ContactDto Contact { get; init; } = default!;
    public IReadOnlyList<AddressContactDto> Residencies { get; init; } = [];
    public IReadOnlyList<MonthlyStatementDto> Statements { get; init; } = [];
}

public class StatementsIndexViewModel
{
    /// <summary>Selected month, or null when showing all periods.</summary>
    public BillingPeriod? Period { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<BillingPeriod> PeriodOptions { get; init; } = [];
    public IReadOnlyList<MonthlyStatementDto> Statements { get; init; } = [];
    public PeriodOverview? Overview { get; init; }
    /// <summary>Problems reported by the last generation run, shown once after redirect.</summary>
    public IReadOnlyList<AllocationWarning> AllocationWarnings { get; init; } = [];
}

public class OverviewViewModel
{
    public PeriodOverview Overview { get; init; } = default!;
    public IReadOnlyList<BillingPeriod> PeriodOptions { get; init; } = [];
    public bool HasAddresses { get; init; }
    public bool HasServices { get; init; }
    public bool HasContacts { get; init; }
    public bool HasResidents { get; init; }
    public bool SetupComplete => HasAddresses && HasServices && HasContacts && HasResidents;
}
