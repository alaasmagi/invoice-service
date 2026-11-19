using System.Text.Json.Serialization;

namespace Contracts.External;

/// <summary>
/// Content of ai-hub's <c>invoice-parsed</c> event. Dates and amounts stay strings so a malformed value fails the
/// document instead of rejecting the message.
/// </summary>
public class InvoiceParsedContent
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; init; } = default!;

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; init; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; init; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; init; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("rows")]
    public List<InvoiceParsedRowContent> Rows { get; init; } = [];
}

public class InvoiceParsedRowContent
{
    [JsonPropertyName("addressText")]
    public string? AddressText { get; init; }

    [JsonPropertyName("amount")]
    public string? Amount { get; init; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; init; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; init; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; init; }
}
