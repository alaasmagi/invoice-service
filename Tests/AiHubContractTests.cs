using Base.Message;
using Contracts.External;
using System.Text.Json;
using Xunit;

namespace Tests;

public class AiHubContractTests
{
    private const string DocumentId = "c4f1d2e3-0a1b-4c5d-8e9f-0a1b2c3d4e5f";

    [Fact]
    public void ParseCommand_SerializesContractFieldNames()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 18, 11, 15, 30, TimeSpan.Zero);
        var envelope = BaseEventEnvelope<InvoiceParseCommandContent>.CreateCommand(
            "invoice-service", "invoice-service", "invoice-parse", "1.0",
            new InvoiceParseCommandContent
            {
                DocumentId = DocumentId,
                FileUrl = "https://signed.example/file.pdf",
                FileName = "telia.pdf",
                Sha256 = new string('a', 64)
            },
            expiresAt);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(envelope));
        var root = json.RootElement;
        var content = root.GetProperty("content");

        Assert.Equal("invoice-service.invoice-service.invoice-parse", BaseRoutingKey.For(envelope));
        Assert.Equal(JsonValueKind.String, root.GetProperty("expiresAt").ValueKind);
        Assert.Equal(DocumentId, content.GetProperty("documentId").GetString());
        Assert.Equal("https://signed.example/file.pdf", content.GetProperty("fileUrl").GetString());
        Assert.Equal("telia.pdf", content.GetProperty("fileName").GetString());
        Assert.Equal("application/pdf", content.GetProperty("contentType").GetString());
        Assert.Equal(new string('a', 64), content.GetProperty("sha256").GetString());
        Assert.Equal(5, content.EnumerateObject().Count());
    }

    [Fact]
    public void ParsedEvent_DeserializesContractExample()
    {
        const string json = """
            {
              "id": "6a0e1b2c-3d4e-4f50-8a1b-2c3d4e5f6a7b",
              "source": "ai-hub",
              "tenant": "invoice-service",
              "action": "invoice-parsed",
              "timestamp": "2026-11-18T10:15:30.123Z",
              "contentVersion": "1.0",
              "content": {
                "documentId": "c4f1d2e3-0a1b-4c5d-8e9f-0a1b2c3d4e5f",
                "serviceName": "Telia",
                "invoiceDate": "2026-11-05",
                "periodStart": "2026-10-01",
                "periodEnd": "2026-10-31",
                "currency": "EUR",
                "rows": [
                  { "addressText": "Tallinn - Tehnika", "amount": "40.00" },
                  { "addressText": "Leesi - Tiiu", "amount": "75.75", "periodStart": "2026-09-15", "periodEnd": null }
                ]
              }
            }
            """;

        var envelope = JsonSerializer.Deserialize<BaseEventEnvelope<InvoiceParsedContent>>(json)!;
        var content = envelope.Content;

        Assert.Null(envelope.ExpiresAt);
        Assert.Equal(DocumentId, content.DocumentId);
        Assert.Equal("Telia", content.ServiceName);
        Assert.Equal("2026-11-05", content.InvoiceDate);
        Assert.Equal("2026-10-01", content.PeriodStart);
        Assert.Equal("2026-10-31", content.PeriodEnd);
        Assert.Equal("EUR", content.Currency);
        Assert.Equal(2, content.Rows.Count);
        Assert.Equal("Tallinn - Tehnika", content.Rows[0].AddressText);
        Assert.Equal("40.00", content.Rows[0].Amount);
        Assert.Null(content.Rows[0].InvoiceDate);
        Assert.Equal("2026-09-15", content.Rows[1].PeriodStart);
        Assert.Null(content.Rows[1].PeriodEnd);
    }

    [Fact]
    public void ParsedEvent_KeepsMalformedValuesAsStrings()
    {
        // Malformed dates and amounts must reach the handler (which fails the document), not break deserialization.
        const string json = """
            { "documentId": "c4f1d2e3-0a1b-4c5d-8e9f-0a1b2c3d4e5f", "invoiceDate": "05.11.2026",
              "rows": [ { "addressText": "Tehnika", "amount": "40,00 €" } ] }
            """;

        var content = JsonSerializer.Deserialize<InvoiceParsedContent>(json)!;

        Assert.Equal("05.11.2026", content.InvoiceDate);
        Assert.Equal("40,00 €", content.Rows[0].Amount);
    }

    [Fact]
    public void FailedEvent_DeserializesContractFields()
    {
        const string json = """
            { "documentId": "c4f1d2e3-0a1b-4c5d-8e9f-0a1b2c3d4e5f", "reason": "not-an-invoice", "message": "The file is a receipt." }
            """;

        var content = JsonSerializer.Deserialize<InvoiceParseFailedContent>(json)!;

        Assert.Equal(DocumentId, content.DocumentId);
        Assert.Equal("not-an-invoice", content.Reason);
        Assert.Equal("The file is a receipt.", content.Message);
    }
}
