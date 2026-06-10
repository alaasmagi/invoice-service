namespace Contracts.Application;

public interface IPdfInvoiceProviderDetector
{
    PdfInvoiceProviderDetectionResult Detect(string text);
}
