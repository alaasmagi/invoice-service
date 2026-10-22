namespace Contracts.Application;

/// <summary>
/// Outcome of publishing one invoice email command. <see cref="Confirmed"/> is true only when the
/// broker confirmed the publish (publisher confirms + mandatory return). A publish that is not
/// confirmed must be treated as failed and must NOT be recorded as sent.
/// </summary>
public record InvoiceEmailPublishResult(bool Confirmed, string EnvelopeId, string? FailureReason)
{
    public static InvoiceEmailPublishResult Ok(string envelopeId) => new(true, envelopeId, null);

    public static InvoiceEmailPublishResult Failed(string envelopeId, string reason) => new(false, envelopeId, reason);
}
