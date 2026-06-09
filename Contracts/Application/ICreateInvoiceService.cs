using Domain;

namespace Contracts.Application;

public interface ICreateInvoiceService
{
    Task<Invoice> CreateAsync(Invoice invoice, Guid userId);
}
