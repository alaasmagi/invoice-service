using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IMonthlyStatementRepository : IBaseRepository<MonthlyStatement>
{
    Task RemoveByIdsAsync(IReadOnlyCollection<Guid> monthlyStatementIds, Guid userId);

    Task SetSendStatusAsync(Guid monthlyStatementId, Guid userId, EMonthlyStatementStatus status, DateTime? sentAt);

    /// <summary>Stages a regenerated total and marks the statement ready to send again; the caller saves.</summary>
    Task MarkRegeneratedAsync(Guid monthlyStatementId, Guid userId, decimal totalSum);
}
