namespace Contracts.Application;

public sealed class MonthlyStatementSenderPaymentDetails
{
    public string FullName { get; init; } = default!;
    public string Locale { get; init; } = "et";
    public string BankIban { get; init; } = default!;
}
