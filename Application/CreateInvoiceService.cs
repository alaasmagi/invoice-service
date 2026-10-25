using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class CreateInvoiceService(
    IInvoiceRepository invoiceRepository,
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

        // How the invoice is split is decided at statement generation (manual split, split rule or equal split).
        await uow.SaveChangesAsync();
        return invoiceResponse.Value;
    }
}
