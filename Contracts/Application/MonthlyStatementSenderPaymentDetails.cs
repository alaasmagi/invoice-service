namespace Contracts.Application;

public sealed class MonthlyStatementSenderPaymentDetails
{
    public string Fullname { get; init; } = default!;
    public string BankIban { get; init; } = default!;
}
