namespace Application;

public sealed class BrevoEmailOptions
{
    public string ApiKey { get; init; } = default!;
    public string SenderEmail { get; init; } = default!;
    public string SenderName { get; init; } = default!;
    public string BankAccountName { get; init; } = default!;
    public string BankIban { get; init; } = default!;
}
