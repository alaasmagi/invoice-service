namespace Contracts.External;

/// <summary>Outcome of one parse request. Only a broker-confirmed publish counts as sent.</summary>
public class InvoiceParseRequestResult
{
    public bool Confirmed { get; init; }

    public string? FailureReason { get; init; }

    public static InvoiceParseRequestResult Ok() => new() { Confirmed = true };

    public static InvoiceParseRequestResult Failed(string reason) => new() { FailureReason = reason };
}
