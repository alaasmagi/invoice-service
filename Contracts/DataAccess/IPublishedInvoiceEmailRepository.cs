using Domain;

namespace Contracts.DataAccess;

public interface IPublishedInvoiceEmailRepository
{
    /// <summary>True when an invoice email for this statement has already been published and confirmed.</summary>
    Task<bool> ExistsForStatementAsync(Guid monthlyStatementId, Guid userId, CancellationToken ct = default);

    /// <summary>Records a confirmed publish. Call only after the broker confirms the message.</summary>
    Task AddAsync(PublishedInvoiceEmail record, CancellationToken ct = default);
}
