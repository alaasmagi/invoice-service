using System.Text.Json.Serialization;

namespace Contracts.External;

/// <summary>Content of ai-hub's <c>invoice-parse-failed</c> event.</summary>
public class InvoiceParseFailedContent
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; init; } = default!;

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
