using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IInvoiceDocumentRepository : IBaseRepository<InvoiceDocument>
{
    Task<InvoiceDocument?> FindBySha256Async(Guid userId, string sha256);

    /// <summary>The document with its rows. Without <paramref name="userId"/> any owner's document is read (message handlers).</summary>
    Task<InvoiceDocument?> GetWithRowsAsync(Guid id, Guid userId = default);
}
