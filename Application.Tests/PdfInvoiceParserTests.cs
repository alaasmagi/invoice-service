using Application;
using Xunit;

namespace Application.Tests;

public sealed class PdfInvoiceParserTests
{
    [Fact]
    public void TeliaParserReadsAddressAmountAndDates()
    {
        var text = File.ReadAllText(Path.Combine("Fixtures", "telia-sample.txt"));
        var document = new TeliaPdfInvoiceParser().Parse(text);

        var row = Assert.Single(document.Rows);
        Assert.Equal("Telia", document.ServiceName);
        Assert.Equal(new DateOnly(2026, 6, 10), document.InvoiceDate);
        Assert.Equal(new DateOnly(2026, 5, 1), document.PeriodStart);
        Assert.Equal(new DateOnly(2026, 5, 31), document.PeriodEnd);
        Assert.Equal("Leesi - Tiiu", row.AddressText);
        Assert.Equal(40.00m, row.Amount);
    }

    [Fact]
    public void EnefitParserReadsMultipleAddressRows()
    {
        var document = new EnefitPdfInvoiceParser().Parse("""
            Enefit AS
            Arve kuupäev 12.06.2026
            Periood 01.05.2026 - 31.05.2026
            Tarbimiskoht: Tallinn - Tehnika
            Elektrienergia 23,34 €
            Võrguteenus 30,28 €
            Arve summa: 62,45 €
            Tarbimiskoht: Leesi - Tiiu
            Elektrienergia 18,10 €
            Arve summa: 18,10 €
            """);

        Assert.Equal("Enefit", document.ServiceName);
        Assert.Equal(2, document.Rows.Count);
        Assert.Contains(document.Rows, row => row.AddressText == "Tallinn - Tehnika" && row.Amount == 62.45m);
        Assert.Contains(document.Rows, row => row.AddressText == "Leesi - Tiiu" && row.Amount == 18.10m);
    }

    [Fact]
    public void EnefitParserIgnoresServiceAmountLinesWithoutAddressContext()
    {
        var document = new EnefitPdfInvoiceParser().Parse("""
            Enefit AS
            Elektrienergia 23,34 €
            Tasakaalustamisvõimsuse kulu 1,28 €
            Võrguteenus 30,28 €
            Arve summa: 75,75 €
            """);

        Assert.Empty(document.Rows);
    }

    [Fact]
    public void TeliaParserReadsAmountBeforeTrailingText()
    {
        var document = new TeliaPdfInvoiceParser().Parse("""
            Telia Eesti AS
            Arve kuupäev 10.06.2026
            Periood 01.05.2026 - 31.05.2026
            Leesi 40,00 € Telia teenused
            """);

        var row = Assert.Single(document.Rows);
        Assert.Equal("Leesi", row.AddressText);
        Assert.Equal(40.00m, row.Amount);
    }

    [Fact]
    public void TeliaParserReadsOnlyAddressSubtotalRowsFromRealisticLines()
    {
        var document = new TeliaPdfInvoiceParser().Parse("""
            Telia Eesti AS
            Arve kuupäev 10.06.2026
            Periood 01.05.2026 - 31.05.2026
            Kodused teenused 51,47 €
            HARJU MAAKOND KUUSALU VALD LEESI 17,976 22,29
            Koduinternet 40M / 40M, kuutasu 31 päeva 15,573 24% 19,310
            HARJU MAAKOND TALLINN KESKLINN 11,250 13,95
            Mobiiltelefoni number 5104543 Viljar Kõverik 4,170 5,17
            """);

        Assert.Equal(2, document.Rows.Count);
        Assert.Contains(document.Rows, row => row.AddressText == "HARJU MAAKOND KUUSALU VALD LEESI" && row.Amount == 22.29m);
        Assert.Contains(document.Rows, row => row.AddressText == "HARJU MAAKOND TALLINN KESKLINN" && row.Amount == 13.95m);
    }
}
