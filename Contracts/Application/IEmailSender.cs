namespace Contracts.Application;

public interface IEmailSender
{
    Task SendMonthlyStatementEmailAsync(string toEmail, string contactName, decimal amount, string period);
}
