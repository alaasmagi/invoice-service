using System.Text.RegularExpressions;
using Contracts.Application;

namespace Application;

public sealed partial class EnefitPdfInvoiceParser : PdfInvoiceParserBase
{
    public override EPdfInvoiceProvider Provider => EPdfInvoiceProvider.Enefit;
    protected override string ServiceName => "Enefit";

    public override PdfInvoiceParsedDocument Parse(string text)
    {
        var invoiceDate = FindInvoiceDate(text);
        var (periodStart, periodEnd) = FindPeriod(text);
        var document = new PdfInvoiceParsedDocument
        {
            Provider = Provider,
            ServiceName = ServiceName,
            InvoiceDate = invoiceDate,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        string? currentAddress = null;
        foreach (var line in SplitLines(text))
        {
            var normalizedLine = PdfInvoiceNormalization.NormalizeKey(line);
            var address = TryExtractAddress(line, normalizedLine);
            if (!string.IsNullOrWhiteSpace(address))
            {
                currentAddress = address;
                continue;
            }

            if (currentAddress == null || !IsSectionTotalLine(normalizedLine))
            {
                continue;
            }

            var match = AmountRegex().Matches(line).Cast<Match>()
                .Where(candidate => TryParseAmount(candidate.Groups["amount"].Value, out _))
                .LastOrDefault();
            if (match == null || !TryParseAmount(match.Groups["amount"].Value, out var amount))
            {
                continue;
            }

            document.Rows.Add(new PdfInvoiceParsedRow
            {
                AddressText = currentAddress,
                Amount = amount,
                InvoiceDate = invoiceDate,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                SourceText = line
            });
            currentAddress = null;
        }

        if (document.Rows.Count == 0)
        {
            document.Warnings.Add("No address amount rows were found for Enefit. Check that the PDF text contains a consumption point/address label before the amount summary.");
        }

        return document;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 5);
    }

    private static string? TryExtractAddress(string line, string normalizedLine)
    {
        if (normalizedLine.Contains("tarbimiskoht", StringComparison.Ordinal)
            || normalizedLine.Contains("mootepunkt", StringComparison.Ordinal)
            || normalizedLine.Contains("aadress", StringComparison.Ordinal))
        {
            var labelMatch = AddressLabelRegex().Match(line);
            var candidate = labelMatch.Success ? labelMatch.Groups["address"].Value : line;
            return CleanAddress(candidate);
        }

        if (normalizedLine.Contains(" maakond ", StringComparison.Ordinal)
            || normalizedLine.StartsWith("harju maakond", StringComparison.Ordinal)
            || normalizedLine.Contains(" tallinn ", StringComparison.Ordinal)
            || normalizedLine.Contains(" leesi ", StringComparison.Ordinal))
        {
            return CleanAddress(line);
        }

        return null;
    }

    private static string CleanAddress(string value)
    {
        var cleaned = AmountRegex().Replace(value, string.Empty);
        cleaned = UnitNoiseRegex().Replace(cleaned, string.Empty);
        cleaned = cleaned.Trim(' ', '-', ':', '|', '\t');
        return cleaned.Length == 0 ? value.Trim() : cleaned;
    }

    private static bool IsSectionTotalLine(string normalizedLine)
    {
        return normalizedLine.StartsWith("arve summa", StringComparison.Ordinal)
               || normalizedLine.StartsWith("kuulub tasumisele", StringComparison.Ordinal)
               || normalizedLine.StartsWith("kokku", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"(?i)(tarbimiskoht|m[õo][õo]tepunkt|aadress)\s*:?\s*(?<address>.+)$")]
    private static partial Regex AddressLabelRegex();

    [GeneratedRegex(@"\b(kwh|mwh|eur|€)\b", RegexOptions.IgnoreCase)]
    private static partial Regex UnitNoiseRegex();
}
