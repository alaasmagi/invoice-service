using Domain;

namespace Contracts.Application;

public interface IGenerateMonthlyStatementService
{
    Task<MonthlyStatement?> GenerateAsync(Guid addressId, int year, int month, Guid userId);
}
