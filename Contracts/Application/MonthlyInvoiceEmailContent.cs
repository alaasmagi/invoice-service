using System.Text.Json.Serialization;

namespace Contracts.Application;

public class MonthlyInvoiceEmailContent
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = default!;

    [JsonPropertyName("fullName")]
    public string FullName { get; init; } = default!;

    [JsonPropertyName("locale")]
    public string Locale { get; init; } = "et";

    [JsonPropertyName("invoiceIdentifier")]
    public string InvoiceIdentifier { get; init; } = default!;

    [JsonPropertyName("invoicePeriod")]
    public string InvoicePeriod { get; init; } = default!;

    [JsonPropertyName("invoiceDate")]
    public string InvoiceDate { get; init; } = default!;

    [JsonPropertyName("invoiceDueDate")]
    public string InvoiceDueDate { get; init; } = default!;

    [JsonPropertyName("invoiceTotal")]
    public string InvoiceTotal { get; init; } = default!;

    // ISO 4217 code, never a symbol. email-hub renders the symbol per locale.
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "EUR";

    [JsonPropertyName("invoiceRows")]
    public IReadOnlyList<MonthlyInvoiceEmailRowContent> InvoiceRows { get; init; } = [];

    [JsonPropertyName("paymentFullName")]
    public string PaymentFullName { get; init; } = default!;

    [JsonPropertyName("paymentIban")]
    public string PaymentIban { get; init; } = default!;

    [JsonPropertyName("paymentDescription")]
    public string PaymentDescription { get; init; } = default!;
}

public class MonthlyInvoiceEmailRowContent
{
    [JsonPropertyName("address")]
    public string Address { get; init; } = default!;

    [JsonPropertyName("service")]
    public string Service { get; init; } = default!;

    [JsonPropertyName("periodStart")]
    public string PeriodStart { get; init; } = default!;

    [JsonPropertyName("periodEnd")]
    public string PeriodEnd { get; init; } = default!;

    [JsonPropertyName("invoiceAmount")]
    public string InvoiceAmount { get; init; } = default!;

    [JsonPropertyName("fractions")]
    public int Fractions { get; init; }

    [JsonPropertyName("invoiceFraction")]
    public string InvoiceFraction { get; init; } = default!;
}
