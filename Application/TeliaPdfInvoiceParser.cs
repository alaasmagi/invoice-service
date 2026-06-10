using Contracts.Application;

namespace Application;

public sealed class TeliaPdfInvoiceParser : PdfInvoiceParserBase
{
    public override EPdfInvoiceProvider Provider => EPdfInvoiceProvider.Telia;
    protected override string ServiceName => "Telia";

    protected override bool ShouldParseAmountLine(string line, string normalizedLine)
    {
        return normalizedLine.Contains("maakond", StringComparison.Ordinal)
               || normalizedLine.StartsWith("tallinn ", StringComparison.Ordinal)
               || normalizedLine.StartsWith("leesi ", StringComparison.Ordinal);
    }
}
