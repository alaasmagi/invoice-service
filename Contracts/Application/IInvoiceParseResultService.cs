using Contracts.External;

namespace Contracts.Application;

/// <summary>
/// Applies ai-hub's parse results to invoice documents. Replies for unknown documents or documents that are not
/// waiting for a result are ignored, so redelivered and late replies are harmless.
/// </summary>
public interface IInvoiceParseResultService
{
    Task ApplyParsedAsync(InvoiceParsedContent content);

    Task ApplyFailedAsync(InvoiceParseFailedContent content);
}
