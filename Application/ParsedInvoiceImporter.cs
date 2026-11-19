using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class ParsedInvoiceImporter(
    IServiceRepository serviceRepository,
    IAddressRepository addressRepository,
    IInvoiceRepository invoiceRepository,
    ICreateInvoiceService createInvoiceService) : IParsedInvoiceImporter
{
    public async Task<IReadOnlyList<InvoiceDocumentRow>> ImportAsync(
        PdfInvoiceParsedDocument parsed,
        Guid userId,
        Guid invoiceDocumentId)
    {
        var service = FindService(await serviceRepository.GetAllForUserAsync(userId), parsed.ServiceName);
        var addresses = await addressRepository.GetAllForUserAsync(userId);
        var rows = new List<InvoiceDocumentRow>();

        foreach (var parsedRow in parsed.Rows)
        {
            var row = new InvoiceDocumentRow
            {
                InvoiceDocumentId = invoiceDocumentId,
                Position = rows.Count + 1,
                AddressText = parsedRow.AddressText,
                Amount = parsedRow.Amount,
                InvoiceDate = parsedRow.InvoiceDate ?? parsed.InvoiceDate,
                PeriodStart = parsedRow.PeriodStart ?? parsed.PeriodStart,
                PeriodEnd = parsedRow.PeriodEnd ?? parsed.PeriodEnd
            };
            rows.Add(row);

            if (string.IsNullOrWhiteSpace(row.AddressText) || row.Amount == null || row.InvoiceDate == null)
            {
                row.Outcome = EInvoiceDocumentRowOutcome.Invalid;
                row.Reason = "Missing required address, amount, or invoice date.";
                continue;
            }

            var addressMatch = FindAddressMatch(addresses, row.AddressText);
            if (service == null || addressMatch.Address == null)
            {
                row.Outcome = EInvoiceDocumentRowOutcome.Unmatched;
                row.Reason = service == null
                    ? $"No matching service was found for '{parsed.ServiceName}'."
                    : addressMatch.IsAmbiguous
                        ? "Multiple matching addresses were found."
                        : "No matching address was found.";
                continue;
            }

            await CreateInvoiceAsync(row, service.Id, addressMatch.Address.Id, userId);
        }

        return rows;
    }

    public async Task CreateInvoiceAsync(InvoiceDocumentRow row, Guid serviceId, Guid addressId, Guid userId)
    {
        var exists = await invoiceRepository.ExistsEquivalentAsync(
            userId, serviceId, addressId, row.InvoiceDate!.Value, row.PeriodStart, row.PeriodEnd, row.Amount!.Value);
        if (exists)
        {
            row.Outcome = EInvoiceDocumentRowOutcome.Duplicate;
            row.Reason = "An equivalent invoice already exists.";
            return;
        }

        var invoice = await createInvoiceService.CreateAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            AddressId = addressId,
            InvoiceDate = row.InvoiceDate.Value,
            PeriodStart = row.PeriodStart,
            PeriodEnd = row.PeriodEnd,
            TotalSum = row.Amount.Value,
            InvoiceDocumentId = row.InvoiceDocumentId
        }, userId);

        row.Outcome = EInvoiceDocumentRowOutcome.Created;
        row.Reason = null;
        row.InvoiceId = invoice.Id;
    }

    private static Service? FindService(IEnumerable<Service> services, string parsedServiceName)
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

    private record AddressMatch(Address? Address, bool IsAmbiguous);
}
