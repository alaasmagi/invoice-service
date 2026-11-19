using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Domain;

namespace Application;

public class InvoiceDocumentService(
    IInvoiceDocumentRepository documentRepository,
    IFileStorage fileStorage,
    IInvoiceParseRequestPublisher parseRequestPublisher,
    IBaseUow uow,
    TimeProvider timeProvider) : IInvoiceDocumentService
{
    // ai-hub gets an hour to fetch the PDF; the command expires with the link (design D4).
    public static readonly TimeSpan ParseLinkLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan DownloadLinkLifetime = TimeSpan.FromMinutes(10);

    public async Task<IMethodResponse<InvoiceDocumentUploadResult>> UploadAsync(
        InvoiceDocumentUploadRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        if (request.Length > InvoicePdf.MaxBytes)
        {
            return MethodResponse<InvoiceDocumentUploadResult>.Failure(InvoicePdf.FileTooLarge());
        }

        var content = await InvoicePdf.ReadBoundedAsync(request.Content, ct);
        var error = InvoicePdf.Validate(content.Length, content);
        if (error != null)
        {
            return MethodResponse<InvoiceDocumentUploadResult>.Failure(error);
        }

        var sha256 = InvoicePdf.Sha256Hex(content);
        var existing = await documentRepository.FindBySha256Async(userId, sha256);
        if (existing != null)
        {
            return MethodResponse<InvoiceDocumentUploadResult>.Success(
                new InvoiceDocumentUploadResult { Document = existing, IsDuplicate = true });
        }

        var now = timeProvider.GetUtcNow();
        var storageKey = InvoicePdfStorageKey.Build(userId, now.Year, sha256);
        await fileStorage.UploadAsync(new MemoryStream(content), storageKey, InvoicePdf.ContentType, ct);

        var created = await documentRepository.CreateAsync(new InvoiceDocument
        {
            Id = Guid.NewGuid(),
            StorageKey = storageKey,
            FileName = Path.GetFileName(request.FileName),
            SizeBytes = content.Length,
            Sha256 = sha256,
            Status = EInvoiceDocumentStatus.Uploaded,
            UploadedAt = now.UtcDateTime
        }, userId);
        if (!created.Successful || created.Value == null)
        {
            throw new InvalidOperationException("Invoice document creation failed.");
        }

        await uow.SaveChangesAsync();

        var document = await RequestParseAsync(created.Value, userId, ct);
        return MethodResponse<InvoiceDocumentUploadResult>.Success(new InvoiceDocumentUploadResult { Document = document });
    }

    public async Task<IMethodResponse<InvoiceDocument>> RetryAsync(Guid documentId, Guid userId, CancellationToken ct = default)
    {
        var found = await documentRepository.GetByIdAsync(documentId, userId);
        if (!found.Successful || found.Value == null)
        {
            return MethodResponse<InvoiceDocument>.Failure(new Error(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound));
        }

        if (!found.Value.CanRetry(timeProvider.GetUtcNow().UtcDateTime))
        {
            return MethodResponse<InvoiceDocument>.Failure(
                new Error(InvoiceDocumentErrors.CannotRetry, "The document is being parsed or has already been parsed."));
        }

        return MethodResponse<InvoiceDocument>.Success(await RequestParseAsync(found.Value, userId, ct));
    }

    public async Task<IMethodResponse<string>> GetDownloadUrlAsync(Guid documentId, Guid userId, CancellationToken ct = default)
    {
        var found = await documentRepository.GetByIdAsync(documentId, userId);
        if (!found.Successful || found.Value == null)
        {
            return MethodResponse<string>.Failure(new Error(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound));
        }

        return MethodResponse<string>.Success(
            await fileStorage.GetSignedUrlAsync(found.Value.StorageKey, DownloadLinkLifetime, ct));
    }

    public async Task<IReadOnlyList<InvoiceDocument>> GetAllAsync(Guid userId)
    {
        var documents = await documentRepository.GetAllAsync(userId);
        return (documents.Value ?? []).OrderByDescending(document => document.UploadedAt).ToList();
    }

    public Task<InvoiceDocument?> GetWithRowsAsync(Guid documentId, Guid userId)
    {
        return documentRepository.GetWithRowsAsync(documentId, userId);
    }

    private async Task<InvoiceDocument> RequestParseAsync(InvoiceDocument document, Guid userId, CancellationToken ct)
    {
        // The command must expire no later than the link, so its expiry is fixed before the link is signed.
        var requestedAt = timeProvider.GetUtcNow();
        var expiresAt = requestedAt.Add(ParseLinkLifetime);
        var fileUrl = await fileStorage.GetSignedUrlAsync(document.StorageKey, ParseLinkLifetime, ct);

        var result = await parseRequestPublisher.PublishAsync(new InvoiceParseCommandContent
        {
            DocumentId = document.Id.ToString(),
            FileUrl = fileUrl,
            FileName = document.FileName,
            ContentType = InvoicePdf.ContentType,
            Sha256 = document.Sha256
        }, expiresAt, ct);

        if (result.Confirmed)
        {
            document.Status = EInvoiceDocumentStatus.Processing;
            document.FailureReason = null;
            document.ParseRequestedAt = requestedAt.UtcDateTime;
            document.ParseExpiresAt = expiresAt.UtcDateTime;
        }
        else
        {
            document.Status = EInvoiceDocumentStatus.Uploaded;
            document.FailureReason = result.FailureReason;
        }

        var updated = await documentRepository.UpdateAsync(document.Id, document, actor: userId);
        if (!updated.Successful)
        {
            throw new InvalidOperationException("Invoice document update failed.");
        }

        await uow.SaveChangesAsync();
        return document;
    }
}
