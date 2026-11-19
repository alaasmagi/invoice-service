namespace Contracts.External;

/// <summary>
/// Sends an invoice PDF to ai-hub for parsing. The request must not be acted on after <c>expiresAt</c>, which is
/// also when the link in <see cref="InvoiceParseCommandContent.FileUrl"/> stops working.
/// </summary>
public interface IInvoiceParseRequestPublisher
{
    Task<InvoiceParseRequestResult> PublishAsync(
        InvoiceParseCommandContent content,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);
}
