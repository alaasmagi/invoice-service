using System.Globalization;
using System.Text.RegularExpressions;
using Contracts.Application;

namespace Application;

public abstract partial class PdfInvoiceParserBase : IPdfInvoiceParser
{
    public abstract EPdfInvoiceProvider Provider { get; }
    protected abstract string ServiceName { get; }

    public virtual PdfInvoiceParsedDocument Parse(string text)
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

        foreach (var line in SplitCandidateLines(text))
        {
            var row = ParseAmountLine(line, invoiceDate, periodStart, periodEnd);
            if (row != null)
            {
                document.Rows.Add(row);
            }
        }

        if (document.Rows.Count == 0)
        {
            document.Warnings.Add($"No address amount rows were found for {ServiceName}.");
        }

        return document;
    }

    protected virtual bool ShouldParseAmountLine(string line, string normalizedLine)
    {
        return true;
    }

    protected virtual string CleanAddressText(string addressText)
    {
        return TrailingAmountRegex().Replace(addressText, string.Empty).Trim(' ', '-', ':', '|', '\t');
    }

    protected virtual bool IsIgnoredAmountLine(string normalizedLine)
    {
        var ignored = new[]
        {
            "kokku", "tasumisele", "summa", "kaibemaks", "km ", "viivis", "saldo", "vahesumma", "total"
        };

        return ignored.Any(keyword => normalizedLine.StartsWith(keyword, StringComparison.Ordinal));
    }

    private IEnumerable<string> SplitCandidateLines(string text)
    {
        return text.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 5);
    }

    private PdfInvoiceParsedRow? ParseAmountLine(string line, DateOnly? invoiceDate, DateOnly? periodStart, DateOnly? periodEnd)
    {
        var normalizedLine = PdfInvoiceNormalization.NormalizeKey(line);
        if (!ShouldParseAmountLine(line, normalizedLine) || IsIgnoredAmountLine(normalizedLine) || DateRegex().IsMatch(line))
        {
            return null;
        }

        var match = AmountRegex().Matches(line).Cast<Match>()
            .Where(candidate => TryParseAmount(candidate.Groups["amount"].Value, out _))
            .LastOrDefault();
        if (match == null || !match.Success || !TryParseAmount(match.Groups["amount"].Value, out var amount))
        {
            return null;
        }

        var addressText = CleanAddressText(line[..match.Index]);
        if (string.IsNullOrWhiteSpace(addressText))
        {
            return null;
        }

        var normalizedAddress = PdfInvoiceNormalization.NormalizeKey(addressText);
        if (IsIgnoredAmountLine(normalizedAddress))
        {
            return null;
        }

        return new PdfInvoiceParsedRow
        {
            AddressText = addressText,
            Amount = amount,
            InvoiceDate = invoiceDate,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            SourceText = line
        };
    }

    protected static DateOnly? FindInvoiceDate(string text)
    {
        var lines = text.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var preferred = lines.FirstOrDefault(line =>
            PdfInvoiceNormalization.NormalizeKey(line).Contains("arve kuupaev", StringComparison.Ordinal)
            || PdfInvoiceNormalization.NormalizeKey(line).Contains("invoice date", StringComparison.Ordinal));

        if (preferred != null && TryFindDate(preferred, out var preferredDate))
        {
            return preferredDate;
        }

        return TryFindDate(text, out var date) ? date : null;
    }

    protected static (DateOnly? Start, DateOnly? End) FindPeriod(string text)
    {
        var match = PeriodRegex().Match(text);
        if (!match.Success)
        {
            return (null, null);
        }

        return TryParseDate(match.Groups["start"].Value, out var start)
               && TryParseDate(match.Groups["end"].Value, out var end)
            ? (start, end)
            : (null, null);
    }

    private static bool TryFindDate(string text, out DateOnly date)
    {
        var match = DateRegex().Match(text);
        if (match.Success && TryParseDate(match.Value, out date))
        {
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        var formats = new[] { "d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    protected static bool TryParseAmount(string value, out decimal amount)
    {
        var normalized = value.Replace(" ", string.Empty).Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    [GeneratedRegex(@"(?<amount>-?\d{1,3}(?:[ .]\d{3})*(?:,\d{2})|-?\d+(?:[,.]\d{2}))\s*(?:€|eur)?", RegexOptions.IgnoreCase)]
    protected static partial Regex AmountRegex();

    [GeneratedRegex(@"\s+-?\d+(?:[,.]\d{2,3})\s*$")]
    private static partial Regex TrailingAmountRegex();

    [GeneratedRegex(@"\d{1,2}[./]\d{1,2}[./]\d{4}|\d{4}-\d{1,2}-\d{1,2}")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"(?<start>\d{1,2}[./]\d{1,2}[./]\d{4}|\d{4}-\d{1,2}-\d{1,2})\s*(?:-|–|kuni|to)\s*(?<end>\d{1,2}[./]\d{1,2}[./]\d{4}|\d{4}-\d{1,2}-\d{1,2})", RegexOptions.IgnoreCase)]
    private static partial Regex PeriodRegex();
}
