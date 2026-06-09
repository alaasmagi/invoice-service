using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class CreateInvoiceService(
    IInvoiceRepository invoiceRepository,
    IAddressContactRepository addressContactRepository,
    IInvoiceAllocationRepository invoiceAllocationRepository,
    IBaseUow uow) : ICreateInvoiceService
{
    public async Task<Invoice> CreateAsync(Invoice invoice, Guid userId)
    {
        invoice.UserId = userId;
        if (invoice.Id == Guid.Empty)
        {
            invoice.Id = Guid.NewGuid();
        }

        var invoiceResponse = await invoiceRepository.CreateAsync(invoice, userId);
        if (!invoiceResponse.Successful || invoiceResponse.Value == null)
        {
            throw new InvalidOperationException("Invoice creation failed.");
        }

        var contactsResponse = await addressContactRepository.GetAllAsync(userId);
        var activeContacts = contactsResponse.Successful && contactsResponse.Value != null
            ? contactsResponse.Value
                .Where(addressContact => addressContact.AddressId == invoice.AddressId && addressContact.IsActive(invoice.InvoiceDate))
                .OrderBy(addressContact => addressContact.ContactId)
                .ToList()
            : new List<AddressContact>();

        var allocations = CreateEqualAllocations(invoice, activeContacts, userId);
        foreach (var allocation in allocations)
        {
            await invoiceAllocationRepository.CreateAsync(allocation, userId);
            invoice.Allocations.Add(allocation);
        }

        await uow.SaveChangesAsync();
        return invoiceResponse.Value;
    }

    private static IEnumerable<InvoiceAllocation> CreateEqualAllocations(
        Invoice invoice,
        IReadOnlyList<AddressContact> activeContacts,
        Guid userId)
    {
        if (activeContacts.Count == 0)
        {
            return [];
        }

        var baseAmount = decimal.Round(invoice.TotalSum / activeContacts.Count, 2, MidpointRounding.AwayFromZero);
        var allocations = new List<InvoiceAllocation>();
        var allocated = 0m;

        for (var i = 0; i < activeContacts.Count; i++)
        {
            var amount = i == activeContacts.Count - 1
                ? invoice.TotalSum - allocated
                : baseAmount;

            allocated += amount;
            allocations.Add(new InvoiceAllocation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InvoiceId = invoice.Id,
                ContactId = activeContacts[i].ContactId,
                AllocatedSum = amount,
                CreatedAt = DateTime.UtcNow
            });
        }

        return allocations;
    }
}
