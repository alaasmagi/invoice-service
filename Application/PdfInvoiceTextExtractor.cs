using Contracts.Application;
using UglyToad.PdfPig;

namespace Application;

public sealed class PdfInvoiceTextExtractor : IPdfInvoiceTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var pages = document.GetPages().OrderBy(page => page.Number).Select(page =>
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
            {
                return page.Text;
            }

            var reconstructedLines = words
                .GroupBy(word => Math.Round(word.BoundingBox.Bottom / 2) * 2)
                .OrderByDescending(group => group.Key)
                .Select(group => string.Join(" ", group
                    .OrderBy(word => word.BoundingBox.Left)
                    .Select(word => word.Text)))
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, reconstructedLines)
                   + Environment.NewLine
                   + page.Text;
        });

        return Task.FromResult(string.Join(Environment.NewLine, pages));
    }
}
