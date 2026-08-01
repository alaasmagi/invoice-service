using Domain;

namespace Contracts.DataAccess;

public interface IProcessedInboxMessageRepository
{
    /// <summary>
    /// Inserts the envelope id (unique index). Returns false when the id already exists — i.e. this
    /// is a duplicate delivery and the message should be acked without re-applying. Any other error
    /// propagates. Participates in the caller's ambient transaction.
    /// </summary>
    Task<bool> TryInsertAsync(ProcessedInboxMessage record, CancellationToken ct = default);
}
