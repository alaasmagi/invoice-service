namespace Contracts.Application;

public interface IMonthlyStatementSenderPaymentDetailsProvider
{
    Task<MonthlyStatementSenderPaymentDetails?> GetAsync(Guid userId);
}
