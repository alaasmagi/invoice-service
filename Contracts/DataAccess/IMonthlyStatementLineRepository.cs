using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IMonthlyStatementLineRepository : IBaseRepository<MonthlyStatementLine>
{
    Task RemoveByStatementIdsAsync(IReadOnlyCollection<Guid> monthlyStatementIds, Guid userId);
}
