namespace Contracts.Application;

public interface IPdfInvoiceTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream);
}
