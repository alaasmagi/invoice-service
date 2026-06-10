using Application;
using Contracts.Application;
using Xunit;

namespace Application.Tests;

public sealed class PdfInvoiceProviderDetectorTests
{
    private readonly PdfInvoiceProviderDetector _detector = new();

    [Fact]
    public void DetectsTelia()
    {
        var result = _detector.Detect("Telia Eesti AS arve");

        Assert.Equal(EPdfInvoiceProvider.Telia, result.Provider);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void DetectsEnefit()
    {
        var result = _detector.Detect("Enefit elektriarve");

        Assert.Equal(EPdfInvoiceProvider.Enefit, result.Provider);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void RejectsAmbiguousProvider()
    {
        var result = _detector.Detect("Telia Enefit");

        Assert.Equal(EPdfInvoiceProvider.Unknown, result.Provider);
        Assert.True(result.IsAmbiguous);
    }
}
