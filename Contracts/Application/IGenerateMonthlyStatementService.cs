using Domain;

namespace Contracts.Application;

public interface IGenerateMonthlyStatementService
{
    Task<IReadOnlyList<MonthlyStatement>> GenerateAsync(int year, int month, Guid userId);
}
