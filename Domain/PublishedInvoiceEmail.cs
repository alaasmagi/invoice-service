using Base.Domain;

namespace Domain;

/// <summary>
/// Local record that an invoice email command was published AND confirmed by the broker for a given
/// statement/period. Drives batch idempotency: a re-run skips statements already recorded here.
/// </summary>
public class PublishedInvoiceEmail : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid MonthlyStatementId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public string InvoiceIdentifier { get; set; } = default!;

    public string Recipient { get; set; } = default!;

    /// <summary>Deterministic envelope id sent to email-hub (its idempotency key).</summary>
    public string EnvelopeId { get; set; } = default!;

    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
