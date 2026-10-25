using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IInvoiceAllocationRepository : IBaseRepository<InvoiceAllocation>
{
    /// <summary>Manual splits (<see cref="InvoiceAllocation.IsOverride"/>) for the given invoices.</summary>
    Task<IReadOnlyList<InvoiceAllocation>> GetOverridesByInvoiceIdsAsync(IReadOnlyCollection<Guid> invoiceIds, Guid userId);
}
