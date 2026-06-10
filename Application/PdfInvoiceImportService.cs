using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public sealed class PdfInvoiceImportService(
    IPdfInvoiceTextExtractor textExtractor,
    IPdfInvoiceProviderDetector providerDetector,
    IEnumerable<IPdfInvoiceParser> parsers,
    IServiceRepository serviceRepository,
    IAddressRepository addressRepository,
    IInvoiceRepository invoiceRepository,
    ICreateInvoiceService createInvoiceService) : IPdfInvoiceImportService
{
    public async Task<PdfInvoiceImportResult> ImportAsync(PdfInvoiceImportRequest request, Guid userId)
    {
        var result = new PdfInvoiceImportResult();
        string text;
        try
        {
            text = await textExtractor.ExtractTextAsync(request.PdfStream);
        }
        catch (Exception ex)
        {
            result.FatalErrors.Add($"PDF text extraction failed: {ex.Message}");
            return result;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            result.FatalErrors.Add("PDF text extraction returned no readable text.");
            return result;
        }

        var detection = providerDetector.Detect(text);
        if (detection.Provider == EPdfInvoiceProvider.Unknown)
        {
            result.FatalErrors.Add(detection.Reason ?? "Unsupported or ambiguous PDF provider.");
            return result;
        }

        result.Provider = detection.Provider;
        result.ProviderName = detection.Provider.ToString();
        var parser = parsers.FirstOrDefault(candidate => candidate.Provider == detection.Provider);
        if (parser == null)
        {
            result.FatalErrors.Add($"No parser is registered for {detection.Provider}.");
            return result;
        }

        PdfInvoiceParsedDocument parsed;
        try
        {
            parsed = parser.Parse(text);
        }
        catch (Exception ex)
        {
            result.FatalErrors.Add($"{detection.Provider} parsing failed: {ex.Message}");
            return result;
        }

        result.Warnings.AddRange(parsed.Warnings);
        var services = await serviceRepository.GetAllForUserAsync(userId);
        var service = FindService(services, parsed.ServiceName);
        if (service == null)
        {
            result.FatalErrors.Add($"No matching service was found for '{parsed.ServiceName}'. Create a service with this provider name first.");
            return result;
        }

        var addresses = await addressRepository.GetAllForUserAsync(userId);
        foreach (var row in parsed.Rows)
        {
            var rowInvoiceDate = row.InvoiceDate ?? parsed.InvoiceDate;
            var rowPeriodStart = row.PeriodStart ?? parsed.PeriodStart;
            var rowPeriodEnd = row.PeriodEnd ?? parsed.PeriodEnd;
            if (string.IsNullOrWhiteSpace(row.AddressText) || row.Amount == null || rowInvoiceDate == null)
            {
                result.SkippedRows.Add(new PdfInvoiceSkippedRow
                {
                    AddressText = row.AddressText,
                    Amount = row.Amount,
                    SourceText = row.SourceText,
                    Reason = "Missing required address, amount, or invoice date."
                });
                continue;
            }

            var addressMatch = FindAddressMatch(addresses, row.AddressText);
            if (addressMatch.Address == null)
            {
                result.SkippedRows.Add(new PdfInvoiceSkippedRow
                {
                    AddressText = row.AddressText,
                    Amount = row.Amount,
                    SourceText = row.SourceText,
                    Reason = addressMatch.IsAmbiguous
                        ? "Multiple matching addresses were found."
                        : "No matching address was found."
                });
                continue;
            }

            var address = addressMatch.Address;
            var exists = await invoiceRepository.ExistsEquivalentAsync(
                userId,
                service.Id,
                address.Id,
                rowInvoiceDate.Value,
                rowPeriodStart,
                rowPeriodEnd,
                row.Amount.Value);
            if (exists)
            {
                result.SkippedRows.Add(new PdfInvoiceSkippedRow
                {
                    AddressText = row.AddressText,
                    Amount = row.Amount,
                    SourceText = row.SourceText,
                    Reason = "An equivalent invoice already exists."
                });
                continue;
            }

            var invoice = await createInvoiceService.CreateAsync(new Invoice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServiceId = service.Id,
                AddressId = address.Id,
                InvoiceDate = rowInvoiceDate.Value,
                PeriodStart = rowPeriodStart,
                PeriodEnd = rowPeriodEnd,
                TotalSum = row.Amount.Value
            }, userId);

            result.CreatedInvoices.Add(new PdfInvoiceCreatedInvoice
            {
                InvoiceId = invoice.Id,
                AddressId = address.Id,
                AddressName = address.Name,
                ServiceId = service.Id,
                ServiceName = service.Name,
                InvoiceDate = rowInvoiceDate.Value,
                PeriodStart = rowPeriodStart,
                PeriodEnd = rowPeriodEnd,
                TotalSum = row.Amount.Value
            });
        }

        if (parsed.Rows.Count == 0 && result.FatalErrors.Count == 0)
        {
            result.FatalErrors.Add("No importable address rows were found in the PDF.");
        }

        return result;
    }

    public static Service? FindService(IEnumerable<Service> services, string parsedServiceName)
    {
        var parsedKey = PdfInvoiceNormalization.NormalizeKey(parsedServiceName);
        return services.FirstOrDefault(service =>
        {
            var serviceKey = PdfInvoiceNormalization.NormalizeKey(service.Name);
            return serviceKey == parsedKey
                   || serviceKey.Contains(parsedKey, StringComparison.Ordinal)
                   || parsedKey.Contains(serviceKey, StringComparison.Ordinal);
        });
    }

    public static Address? FindAddress(IEnumerable<Address> addresses, string parsedAddressText)
    {
        return FindAddressMatch(addresses, parsedAddressText).Address;
    }

    private static AddressMatch FindAddressMatch(IEnumerable<Address> addresses, string parsedAddressText)
    {
        var parsedKey = PdfInvoiceNormalization.NormalizeKey(parsedAddressText);
        if (string.IsNullOrWhiteSpace(parsedKey))
        {
            return new AddressMatch(null, false);
        }

        var rankedMatches = addresses
            .Select(address => new
            {
                Address = address,
                NameKey = PdfInvoiceNormalization.NormalizeKey(address.Name),
                FullAddressKey = PdfInvoiceNormalization.NormalizeKey(address.FullAddress)
            })
            .Select(candidate => new
            {
                candidate.Address,
                Rank = AddressMatchRank(parsedKey, candidate.NameKey, candidate.FullAddressKey)
            })
            .Where(candidate => candidate.Rank > 0)
            .OrderByDescending(candidate => candidate.Rank)
            .ToList();

        if (rankedMatches.Count == 0)
        {
            return new AddressMatch(null, false);
        }

        var bestRank = rankedMatches[0].Rank;
        var bestMatches = rankedMatches.Where(candidate => candidate.Rank == bestRank).ToList();
        return bestMatches.Count == 1
            ? new AddressMatch(bestMatches[0].Address, false)
            : new AddressMatch(null, true);
    }

    private static int AddressMatchRank(string parsedKey, string nameKey, string fullAddressKey)
    {
        if (nameKey == parsedKey || fullAddressKey == parsedKey)
        {
            return 100;
        }

        if (nameKey.Contains(parsedKey, StringComparison.Ordinal))
        {
            return 80;
        }

        if (fullAddressKey.Contains(parsedKey, StringComparison.Ordinal))
        {
            return 60;
        }

        if (parsedKey.Contains(nameKey, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(nameKey))
        {
            return 40;
        }

        if (parsedKey.Contains(fullAddressKey, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(fullAddressKey))
        {
            return 20;
        }

        if (HasMeaningfulTokenOverlap(parsedKey, nameKey))
        {
            return 10;
        }

        if (HasMeaningfulTokenOverlap(parsedKey, fullAddressKey))
        {
            return 5;
        }

        return 0;
    }

    private static bool HasMeaningfulTokenOverlap(string parsedKey, string addressKey)
    {
        var ignored = new HashSet<string>(StringComparer.Ordinal)
        {
            "harju", "maakond", "vald", "linn", "kesklinn", "tanuv", "tn", "tee", "maja", "korter"
        };
        var parsedTokens = parsedKey
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 4 && !ignored.Contains(token))
            .ToHashSet(StringComparer.Ordinal);
        if (parsedTokens.Count == 0)
        {
            return false;
        }

        return addressKey
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(token => parsedTokens.Contains(token));
    }

    private sealed record AddressMatch(Address? Address, bool IsAmbiguous);
}
