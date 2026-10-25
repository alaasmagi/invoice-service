using Domain;

namespace Contracts.Application;

public interface IGenerateMonthlyStatementService
{
    Task<MonthlyStatementGenerationResult> GenerateAsync(int year, int month, Guid userId);
}

public class MonthlyStatementGenerationResult
{
    public IReadOnlyList<MonthlyStatement> Statements { get; set; } = [];

    public IReadOnlyList<AllocationWarning> Warnings { get; set; } = [];
}
