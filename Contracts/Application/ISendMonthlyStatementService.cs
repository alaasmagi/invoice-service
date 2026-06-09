namespace Contracts.Application;

public interface ISendMonthlyStatementService
{
    Task SendAsync(Guid monthlyStatementId, Guid userId);
}
