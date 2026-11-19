using Domain;

namespace Contracts.Application;

/// <summary>
/// Turns a parsed invoice document into invoices: matches the service and each row's address to the user's records,
/// skips equivalent existing invoices and creates the rest.
/// </summary>
public interface IParsedInvoiceImporter
{
    /// <summary>Returns one row per parsed row, in order, with its outcome; created invoices link to the document.</summary>
    Task<IReadOnlyList<InvoiceDocumentRow>> ImportAsync(PdfInvoiceParsedDocument parsed, Guid userId, Guid invoiceDocumentId);

    /// <summary>Creates the row's invoice for the given service and address unless an equivalent one exists, and sets the row's outcome.</summary>
    Task CreateInvoiceAsync(InvoiceDocumentRow row, Guid serviceId, Guid addressId, Guid userId);
}
