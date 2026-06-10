using Domain;

namespace Contracts.Application;

public enum EPdfInvoiceProvider
{
    Unknown = 0,
    Telia = 1,
    Enefit = 2
}

public sealed class PdfInvoiceImportRequest
{
    public Stream PdfStream { get; init; } = default!;
    public string FileName { get; init; } = default!;
    public long Length { get; init; }
}

public sealed class PdfInvoiceImportResult
{
    public EPdfInvoiceProvider Provider { get; set; } = EPdfInvoiceProvider.Unknown;
    public string? ProviderName { get; set; }
    public bool Success => FatalErrors.Count == 0;
    public List<PdfInvoiceCreatedInvoice> CreatedInvoices { get; } = [];
    public List<PdfInvoiceSkippedRow> SkippedRows { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> FatalErrors { get; } = [];
}

public sealed class PdfInvoiceParsedDocument
{
    public EPdfInvoiceProvider Provider { get; init; }
    public string ServiceName { get; init; } = default!;
    public DateOnly? InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public List<PdfInvoiceParsedRow> Rows { get; } = [];
    public List<string> Warnings { get; } = [];
}

public sealed class PdfInvoiceParsedRow
{
    public string AddressText { get; init; } = default!;
    public decimal? Amount { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public string? SourceText { get; init; }
}

public sealed class PdfInvoiceCreatedInvoice
{
    public Guid InvoiceId { get; init; }
    public Guid AddressId { get; init; }
    public string AddressName { get; init; } = default!;
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = default!;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public decimal TotalSum { get; init; }
}

public sealed class PdfInvoiceSkippedRow
{
    public string AddressText { get; init; } = default!;
    public string Reason { get; init; } = default!;
    public decimal? Amount { get; init; }
    public string? SourceText { get; init; }
}

public sealed class PdfInvoiceProviderDetectionResult
{
    public EPdfInvoiceProvider Provider { get; init; }
    public bool IsAmbiguous { get; init; }
    public string? Reason { get; init; }

    public static PdfInvoiceProviderDetectionResult Detected(EPdfInvoiceProvider provider) => new()
    {
        Provider = provider
    };

    public static PdfInvoiceProviderDetectionResult Unsupported(string reason) => new()
    {
        Provider = EPdfInvoiceProvider.Unknown,
        Reason = reason
    };

    public static PdfInvoiceProviderDetectionResult Ambiguous(string reason) => new()
    {
        Provider = EPdfInvoiceProvider.Unknown,
        IsAmbiguous = true,
        Reason = reason
    };
}
