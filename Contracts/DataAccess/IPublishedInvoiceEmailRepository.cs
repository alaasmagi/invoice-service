using Domain;

namespace Contracts.DataAccess;

public interface IPublishedInvoiceEmailRepository
{
    /// <summary>
    /// Number of invoice emails already published and confirmed for this statement. 0 means never sent;
    /// the value doubles as the highest attempt number, so the next send uses <c>count + 1</c>.
    /// </summary>
    Task<int> CountForStatementAsync(Guid monthlyStatementId, Guid userId, CancellationToken ct = default);

    /// <summary>Records a confirmed publish. Call only after the broker confirms the message.</summary>
    Task AddAsync(PublishedInvoiceEmail record, CancellationToken ct = default);
}
