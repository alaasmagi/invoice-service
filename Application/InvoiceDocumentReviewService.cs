using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class InvoiceDocumentReviewService(
    IInvoiceDocumentRepository documentRepository,
    IInvoiceDocumentRowRepository rowRepository,
    IServiceRepository serviceRepository,
    IAddressRepository addressRepository,
    IParsedInvoiceImporter importer,
    IBaseUow uow) : IInvoiceDocumentReviewService
{
    public async Task<IMethodResponse<InvoiceDocument>> ResolveRowAsync(
        Guid documentId,
        Guid rowId,
        Guid serviceId,
        Guid addressId,
        Guid userId)
    {
        var (document, row, error) = await FindUnmatchedRowAsync(documentId, rowId, userId);
        if (error != null)
        {
            return MethodResponse<InvoiceDocument>.Failure(error);
        }

        var services = await serviceRepository.GetAllForUserAsync(userId);
        var addresses = await addressRepository.GetAllForUserAsync(userId);
        if (services.All(service => service.Id != serviceId) || addresses.All(address => address.Id != addressId))
        {
            return MethodResponse<InvoiceDocument>.Failure(NotFound());
        }

        await importer.CreateInvoiceAsync(row!, serviceId, addressId, userId);
        return MethodResponse<InvoiceDocument>.Success(await SaveAsync(document!, row!, userId));
    }

    public async Task<IMethodResponse<InvoiceDocument>> DiscardRowAsync(Guid documentId, Guid rowId, Guid userId)
    {
        var (document, row, error) = await FindUnmatchedRowAsync(documentId, rowId, userId);
        if (error != null)
        {
            return MethodResponse<InvoiceDocument>.Failure(error);
        }

        row!.Outcome = EInvoiceDocumentRowOutcome.Discarded;
        row.Reason = null;
        return MethodResponse<InvoiceDocument>.Success(await SaveAsync(document!, row, userId));
    }

    private async Task<(InvoiceDocument?, InvoiceDocumentRow?, Error?)> FindUnmatchedRowAsync(Guid documentId, Guid rowId, Guid userId)
    {
        var document = await documentRepository.GetWithRowsAsync(documentId, userId);
        var row = document?.Rows.FirstOrDefault(candidate => candidate.Id == rowId);
        if (document == null || row == null)
        {
            return (null, null, NotFound());
        }

        if (document.Status != EInvoiceDocumentStatus.NeedsReview)
        {
            return (null, null, new Error(InvoiceDocumentReviewErrors.NotInReview, "The document is not waiting for review."));
        }

        return row.Outcome == EInvoiceDocumentRowOutcome.Unmatched
            ? (document, row, null)
            : (null, null, new Error(InvoiceDocumentReviewErrors.RowNotUnmatched, "The row has already been handled."));
    }

    private async Task<InvoiceDocument> SaveAsync(InvoiceDocument document, InvoiceDocumentRow row, Guid userId)
    {
        var updatedRow = await rowRepository.UpdateAsync(row.Id, row);
        if (!updatedRow.Successful)
        {
            throw new InvalidOperationException("Invoice document row update failed.");
        }

        if (document.Rows.All(candidate => candidate.Outcome != EInvoiceDocumentRowOutcome.Unmatched))
        {
            document.Status = EInvoiceDocumentStatus.Completed;
            var updatedDocument = await documentRepository.UpdateAsync(document.Id, document, actor: userId);
            if (!updatedDocument.Successful)
            {
                throw new InvalidOperationException("Invoice document update failed.");
            }
        }

        await uow.SaveChangesAsync();
        return document;
    }

    private static Error NotFound() => new(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound);
}
