namespace Contracts.Application;

public interface IEmailSender
{
    Task SendMonthlyStatementEmailAsync(MonthlyStatementEmail email);
}
