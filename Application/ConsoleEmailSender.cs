using Contracts.Application;

namespace Application;

public class ConsoleEmailSender : IEmailSender
{
    public Task SendMonthlyStatementEmailAsync(MonthlyStatementEmail email)
    {
        Console.WriteLine($"Monthly statement email to {email.ContactName} <{email.ToEmail}> for {email.Period}: {email.TotalAmount:C}");
        foreach (var line in email.Lines)
        {
            Console.WriteLine($"- {line.AddressName} / {line.ServiceName} / {line.InvoiceDate:yyyy-MM-dd}: {line.ContactAmount:C} of {line.InvoiceTotal:C} split between {line.ResidentCount}");
        }

        return Task.CompletedTask;
    }
}
