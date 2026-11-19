using System.Globalization;
using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application;

public class InvoiceParseResultService(
    IInvoiceDocumentRepository documentRepository,
    IInvoiceDocumentRowRepository rowRepository,
    IParsedInvoiceImporter importer,
    IBaseUow uow,
    ILogger<InvoiceParseResultService> logger) : IInvoiceParseResultService
{
    private const int MaxReasonLength = 1024;

    public async Task ApplyParsedAsync(InvoiceParsedContent content)
    {
        var document = await FindProcessingDocumentAsync(content.DocumentId);
        if (document == null)
        {
            return;
        }

        var parsed = ToParsedDocument(content, out var invalidReason);
        if (parsed == null)
        {
            await SaveAsync(document, EInvoiceDocumentStatus.Failed, invalidReason);
            return;
        }

        // The owner always comes from the stored document, never from the message.
        var rows = await importer.ImportAsync(parsed, document.UserId, document.Id);
        foreach (var row in rows)
        {
            row.Id = Guid.NewGuid();
            await rowRepository.CreateAsync(row);
        }

        document.ServiceName = Truncate(content.ServiceName, 256);
        var status = rows.Any(row => row.Outcome == EInvoiceDocumentRowOutcome.Unmatched)
            ? EInvoiceDocumentStatus.NeedsReview
            : EInvoiceDocumentStatus.Completed;
        await SaveAsync(document, status, null);
    }

    public async Task ApplyFailedAsync(InvoiceParseFailedContent content)
    {
        var document = await FindProcessingDocumentAsync(content.DocumentId);
        if (document == null)
        {
            return;
        }

        var reason = string.IsNullOrWhiteSpace(content.Message)
            ? content.Reason
            : $"{content.Reason}: {content.Message}";
        await SaveAsync(document, EInvoiceDocumentStatus.Failed, reason ?? "Parsing failed.");
    }

    private async Task<InvoiceDocument?> FindProcessingDocumentAsync(string documentId)
    {
        if (!Guid.TryParse(documentId, out var id))
        {
            logger.LogWarning("Ignoring parse result with malformed document id.");
            return null;
        }

        var document = await documentRepository.GetWithRowsAsync(id);
        if (document?.Status != EInvoiceDocumentStatus.Processing)
        {
            logger.LogInformation(
                "Ignoring parse result for document {DocumentId} in status {Status}.", id, document?.Status.ToString() ?? "missing");
            return null;
        }

        return document;
    }

    private async Task SaveAsync(InvoiceDocument document, EInvoiceDocumentStatus status, string? failureReason)
    {
        document.Status = status;
        document.FailureReason = Truncate(failureReason, MaxReasonLength);
        var updated = await documentRepository.UpdateAsync(document.Id, document);
        if (!updated.Successful)
        {
            throw new InvalidOperationException("Invoice document update failed.");
        }

        await uow.SaveChangesAsync();
    }

    private static PdfInvoiceParsedDocument? ToParsedDocument(InvoiceParsedContent content, out string? invalidReason)
    {
        invalidReason = null;
        if (content.Rows.Count == 0)
        {
            invalidReason = "No invoice rows were found in the PDF.";
            return null;
        }

        if (!TryParseDate(content.InvoiceDate, out var invoiceDate)
            || !TryParseDate(content.PeriodStart, out var periodStart)
            || !TryParseDate(content.PeriodEnd, out var periodEnd))
        {
            invalidReason = "The parse result contains an invalid date.";
            return null;
        }

        var parsed = new PdfInvoiceParsedDocument
        {
            ServiceName = content.ServiceName ?? string.Empty,
            InvoiceDate = invoiceDate,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        foreach (var row in content.Rows)
        {
            if (!TryParseAmount(row.Amount, out var amount)
                || !TryParseDate(row.InvoiceDate, out var rowInvoiceDate)
                || !TryParseDate(row.PeriodStart, out var rowPeriodStart)
                || !TryParseDate(row.PeriodEnd, out var rowPeriodEnd))
            {
                invalidReason = "The parse result contains an invalid date or amount.";
                return null;
            }

            parsed.Rows.Add(new PdfInvoiceParsedRow
            {
                AddressText = row.AddressText ?? string.Empty,
                Amount = amount,
                InvoiceDate = rowInvoiceDate,
                PeriodStart = rowPeriodStart,
                PeriodEnd = rowPeriodEnd
            });
        }

        return parsed;
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        date = null;
        if (value == null)
        {
            return true;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        date = parsed;
        return true;
    }

    private static bool TryParseAmount(string? value, out decimal? amount)
    {
        amount = null;
        if (value == null)
        {
            return true;
        }

        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        amount = parsed;
        return true;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value == null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
