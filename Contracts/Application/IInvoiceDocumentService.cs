using Base.Contracts.DTO;
using Domain;

namespace Contracts.Application;

/// <summary>Stores uploaded invoice PDFs, asks ai-hub to parse them and gives the owner access to them.</summary>
public interface IInvoiceDocumentService
{
    Task<IMethodResponse<InvoiceDocumentUploadResult>> UploadAsync(InvoiceDocumentUploadRequest request, Guid userId, CancellationToken ct = default);

    Task<IMethodResponse<InvoiceDocument>> RetryAsync(Guid documentId, Guid userId, CancellationToken ct = default);

    /// <summary>A short-lived signed link to the document's PDF. Never store or log it.</summary>
    Task<IMethodResponse<string>> GetDownloadUrlAsync(Guid documentId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<InvoiceDocument>> GetAllAsync(Guid userId);

    Task<InvoiceDocument?> GetWithRowsAsync(Guid documentId, Guid userId);
}

public class InvoiceDocumentUploadRequest
{
    public Stream Content { get; init; } = default!;

    public string FileName { get; init; } = default!;

    public long Length { get; init; }
}

public class InvoiceDocumentUploadResult
{
    public InvoiceDocument Document { get; init; } = default!;

    /// <summary>True when the user already had this file; nothing was stored or sent again.</summary>
    public bool IsDuplicate { get; init; }
}

public static class InvoiceDocumentErrors
{
    public const string EmptyFile = "INVOICE_PDF_EMPTY";
    public const string FileTooLarge = "INVOICE_PDF_TOO_LARGE";
    public const string NotPdf = "INVOICE_PDF_INVALID";
    public const string CannotRetry = "INVOICE_DOCUMENT_CANNOT_RETRY";
}
