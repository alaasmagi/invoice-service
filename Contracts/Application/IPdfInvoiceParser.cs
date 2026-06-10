namespace Contracts.Application;

public interface IPdfInvoiceParser
{
    EPdfInvoiceProvider Provider { get; }

    PdfInvoiceParsedDocument Parse(string text);
}
