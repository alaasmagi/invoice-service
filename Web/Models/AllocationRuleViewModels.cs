using Contracts.Application;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models;

public class AllocationRuleIndexViewModel
{
    public IReadOnlyList<AllocationRuleDto> Rules { get; init; } = [];
    public IReadOnlyList<SelectListItem> AddressOptions { get; init; } = [];
    public Guid? AddressId { get; init; }
    public bool ShowPast { get; init; }
    public int PastCount { get; init; }
}

public class AllocationRuleFormViewModel
{
    public AllocationRuleDto Rule { get; init; } = default!;
    public IReadOnlyList<SelectListItem> AddressOptions { get; init; } = [];
    public IReadOnlyList<SelectListItem> ServiceOptions { get; init; } = [];
    public IReadOnlyList<SelectListItem> ContactOptions { get; init; } = [];
    public string? ReturnUrl { get; init; }
}

public class AllocationRuleDetailsViewModel
{
    public AllocationRuleDto Rule { get; init; } = default!;

    /// <summary>The invoice the preview was calculated for, or null when a sample amount was used.</summary>
    public InvoiceDto? PreviewInvoice { get; init; }
    public decimal PreviewTotal { get; init; }
    public InvoiceSplitResult Preview { get; init; } = default!;
    public IReadOnlyDictionary<Guid, string> ContactNames { get; init; } = new Dictionary<Guid, string>();
    public IReadOnlyList<AllocationWarning> PreviewWarnings { get; init; } = [];
}
