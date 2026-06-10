using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IInvoiceRepository : IBaseRepository<Invoice>
{
    Task<bool> ExistsEquivalentAsync(
        Guid userId,
        Guid serviceId,
        Guid addressId,
        DateOnly invoiceDate,
        DateOnly? periodStart,
        DateOnly? periodEnd,
        decimal totalSum);
}
