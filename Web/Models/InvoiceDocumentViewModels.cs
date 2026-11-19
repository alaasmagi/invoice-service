using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models;

public class InvoiceDocumentDetailsViewModel
{
    public InvoiceDocumentDetailsDto Document { get; init; } = default!;

    public IEnumerable<SelectListItem> ServiceOptions { get; init; } = [];

    public IEnumerable<SelectListItem> AddressOptions { get; init; } = [];

    /// <summary>Preselected service for review: the one whose name matches the parsed service, if any.</summary>
    public Guid? SuggestedServiceId { get; init; }
}

public class ApiKeysViewModel
{
    public IReadOnlyList<ApiKeyDto> ApiKeys { get; init; } = [];

    /// <summary>The key just created; shown once and never again.</summary>
    public string? NewPlainKey { get; init; }

    public string? Name { get; init; }
}
