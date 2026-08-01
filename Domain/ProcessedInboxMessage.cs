using Base.Domain;

namespace Domain;

/// <summary>
/// Idempotency guard for consumed messages. The envelope id is stored with a unique index and
/// inserted before the message is applied, so redeliveries (RabbitMQ is at-least-once) are detected
/// and acked silently. A week of retention is enough.
/// </summary>
public class ProcessedInboxMessage : BaseEntity
{
    public string EnvelopeId { get; set; } = default!;

    public string Source { get; set; } = default!;

    public string Action { get; set; } = default!;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
