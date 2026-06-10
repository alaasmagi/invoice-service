namespace Contracts.Application;

public interface IPdfInvoiceImportService
{
    Task<PdfInvoiceImportResult> ImportAsync(PdfInvoiceImportRequest request, Guid userId);
}
