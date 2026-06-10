using Contracts.Application;

namespace Application;

public sealed class PdfInvoiceProviderDetector : IPdfInvoiceProviderDetector
{
    public PdfInvoiceProviderDetectionResult Detect(string text)
    {
        var normalized = PdfInvoiceNormalization.NormalizeKey(text);
        var isTelia = normalized.Contains("telia", StringComparison.Ordinal);
        var isEnefit = normalized.Contains("enefit", StringComparison.Ordinal)
                       || normalized.Contains("eesti energia", StringComparison.Ordinal);

        return (isTelia, isEnefit) switch
        {
            (true, false) => PdfInvoiceProviderDetectionResult.Detected(EPdfInvoiceProvider.Telia),
            (false, true) => PdfInvoiceProviderDetectionResult.Detected(EPdfInvoiceProvider.Enefit),
            (true, true) => PdfInvoiceProviderDetectionResult.Ambiguous("PDF contains both Telia and Enefit markers."),
            _ => PdfInvoiceProviderDetectionResult.Unsupported("PDF does not contain supported Telia or Enefit markers.")
        };
    }
}
