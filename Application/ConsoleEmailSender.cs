using Contracts.Application;

namespace Application;

public class ConsoleEmailSender : IEmailSender
{
    public Task SendMonthlyStatementEmailAsync(string toEmail, string contactName, decimal amount, string period)
    {
        Console.WriteLine($"Monthly statement email to {contactName} <{toEmail}> for {period}: {amount:C}");
        return Task.CompletedTask;
    }
}
