using Contracts.Application;

namespace Application;

public static class MonthlyStatementSenderPaymentDetailsValidator
{
    public static void EnsureCanSend(MonthlyStatementSenderPaymentDetails? details)
    {
        if (details == null)
        {
            throw new InvalidOperationException("Sender profile was not found.");
        }

        if (string.IsNullOrWhiteSpace(details.Fullname))
        {
            throw new InvalidOperationException("Sender full name must be configured before sending monthly statement emails.");
        }

        if (string.IsNullOrWhiteSpace(details.BankIban))
        {
            throw new InvalidOperationException("Sender bank IBAN must be configured before sending monthly statement emails.");
        }
    }
}
