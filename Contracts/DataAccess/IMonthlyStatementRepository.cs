using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IMonthlyStatementRepository : IBaseRepository<MonthlyStatement>
{
    Task RemoveByIdsAsync(IReadOnlyCollection<Guid> monthlyStatementIds, Guid userId);

    Task SetSendStatusAsync(Guid monthlyStatementId, Guid userId, EMonthlyStatementStatus status, DateTime? sentAt);
}
