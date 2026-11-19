using System.Text.Json.Serialization;

namespace Contracts.External;

/// <summary>Content of the <c>invoice-parse</c> command sent to ai-hub (see event-contract.md).</summary>
public class InvoiceParseCommandContent
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; init; } = default!;

    [JsonPropertyName("fileUrl")]
    public string FileUrl { get; init; } = default!;

    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = default!;

    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = "application/pdf";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = default!;
}
