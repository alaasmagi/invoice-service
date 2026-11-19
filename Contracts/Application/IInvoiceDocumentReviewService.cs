using Base.Contracts.DTO;
using Domain;

namespace Contracts.Application;

/// <summary>Lets the owner resolve or discard the rows of a document that could not be matched automatically.</summary>
public interface IInvoiceDocumentReviewService
{
    Task<IMethodResponse<InvoiceDocument>> ResolveRowAsync(Guid documentId, Guid rowId, Guid serviceId, Guid addressId, Guid userId);

    Task<IMethodResponse<InvoiceDocument>> DiscardRowAsync(Guid documentId, Guid rowId, Guid userId);
}

public static class InvoiceDocumentReviewErrors
{
    public const string NotInReview = "INVOICE_DOCUMENT_NOT_IN_REVIEW";
    public const string RowNotUnmatched = "INVOICE_DOCUMENT_ROW_NOT_UNMATCHED";
}
