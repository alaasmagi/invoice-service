using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Messaging;

public sealed class RabbitMqMonthlyInvoiceEmailPublisher(
    IBaseEventPublisher publisher,
    ILogger<RabbitMqMonthlyInvoiceEmailPublisher> logger) : IMonthlyInvoiceEmailPublisher
{
    // Fixed namespace for the deterministic (UUIDv5) envelope id. Re-running a batch reproduces the
    // same id from the same invoice identity + attempt, so email-hub (which dedups on id) drops the repeat.
    private static readonly Guid InvoiceEmailNamespace = new("b1d3f2a4-0c5e-4a6b-8c7d-9e0f1a2b3c4d");

    public async Task<InvoiceEmailPublishResult> PublishAsync(
        MonthlyInvoiceEmailContent content,
        int attempt,
        CancellationToken ct = default)
    {
        var envelope = new BaseEventEnvelope<MonthlyInvoiceEmailContent>
        {
            Id = DeriveDeterministicId(content, attempt),
            Source = MessagingContract.InvoiceSource,
            Tenant = MessagingContract.InvoiceTenant,
            Action = InvoiceMessageActions.Invoice,
            Timestamp = BaseEventEnvelope<MonthlyInvoiceEmailContent>.FormatTimestamp(DateTimeOffset.UtcNow),
            ContentVersion = MessagingContract.InvoiceContentVersion,
            Content = content
        };

        // No expiration: invoice mail must still arrive after an outage.
        // The shared publisher derives the routing key (invoice-service.invoice-service.invoice) via BaseRoutingKey,
        // publishes persistent + mandatory with publisher confirms, and only reports Success when the
        // broker confirms. BasicPublish returning without an exception is not enough.
        var result = await publisher.PublishAsync(envelope, expiration: null, ct);

        if (!result.Success)
        {
            // Log identity only — never the body or paymentIban (personal data).
            logger.LogError(
                "Invoice email not confirmed (id={Id}, source={Source}, tenant={Tenant}, action={Action}): {Reason}",
                envelope.Id, envelope.Source, envelope.Tenant, envelope.Action, result.FailureReason);
            return InvoiceEmailPublishResult.Failed(envelope.Id, result.FailureReason ?? "Publish was not confirmed by the broker.");
        }

        logger.LogInformation(
            "Invoice email confirmed (id={Id}, source={Source}, tenant={Tenant}, action={Action}).",
            envelope.Id, envelope.Source, envelope.Tenant, envelope.Action);
        return InvoiceEmailPublishResult.Ok(envelope.Id);
    }

    private static string DeriveDeterministicId(MonthlyInvoiceEmailContent content, int attempt)
    {
        var identity = $"{content.InvoiceIdentifier}:{content.InvoicePeriod}:{content.Email}";

        // Attempt 1 keeps the original (suffix-free) identity so ids minted before resend support stay
        // stable. Each explicit resend bumps the attempt, changing the id so email-hub delivers it anew.
        if (attempt > 1)
        {
            identity = $"{identity}:{attempt}";
        }

        return CreateUuidV5(InvoiceEmailNamespace, identity).ToString();
    }

    // RFC 4122 name-based UUID v5 (SHA-1). Deterministic: same namespace + name -> same GUID.
    private static Guid CreateUuidV5(Guid namespaceId, string name)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes); // .NET stores the first three groups little-endian; RFC wants big-endian.
        var nameBytes = Encoding.UTF8.GetBytes(name);

        byte[] hash;
        using (var sha1 = SHA1.Create())
        {
            sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = sha1.Hash!;
        }

        var result = new byte[16];
        Array.Copy(hash, 0, result, 0, 16);
        result[6] = (byte)((result[6] & 0x0F) | 0x50); // version 5
        result[8] = (byte)((result[8] & 0x3F) | 0x80); // RFC 4122 variant
        SwapByteOrder(result);
        return new Guid(result);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
