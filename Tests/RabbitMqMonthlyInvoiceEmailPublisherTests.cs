using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace Tests;

public sealed class RabbitMqMonthlyInvoiceEmailPublisherTests
{
    private static MonthlyInvoiceEmailContent SampleContent(string email = "user@example.com") => new()
    {
        Email = email,
        FullName = "testFullName",
        Locale = "et",
        InvoiceIdentifier = "1D58MKI",
        InvoicePeriod = "2026-06",
        InvoiceDate = "2026-06-06",
        InvoiceDueDate = "2026-06-20",
        InvoiceTotal = "97.87",
        Currency = "EUR",
        InvoiceRows =
        [
            new MonthlyInvoiceEmailRowContent
            {
                Address = "Leesi - Tiiu", Service = "Enefit elektriteenus",
                PeriodStart = "2026-05-01", PeriodEnd = "2026-05-31",
                InvoiceAmount = "75.75", Fractions = 1, InvoiceFraction = "75.75"
            },
            new MonthlyInvoiceEmailRowContent
            {
                Address = "Tallinn - Tehnika", Service = "Telia IKT teenused",
                PeriodStart = "2026-05-01", PeriodEnd = "2026-05-31",
                InvoiceAmount = "40.00", Fractions = 2, InvoiceFraction = "20.00"
            }
        ],
        PaymentFullName = "Aleksander Laasmägi",
        PaymentIban = "EE621010011679940224",
        PaymentDescription = "1D58MKI 06.2026"
    };

    private static RabbitMqMonthlyInvoiceEmailPublisher CreateSut(FakeBaseEventPublisher basePublisher) =>
        new(basePublisher, NullLogger<RabbitMqMonthlyInvoiceEmailPublisher>.Instance);

    [Fact]
    public async Task PublishAsync_BuildsInvoiceEnvelope_WithNoExpiration_AndConfirms()
    {
        var basePublisher = new FakeBaseEventPublisher();
        var result = await CreateSut(basePublisher).PublishAsync(SampleContent());

        Assert.True(result.Confirmed);
        Assert.Equal(1, basePublisher.Calls);

        var envelope = basePublisher.LastEnvelope!;
        Assert.Equal(MessagingContract.InvoiceSource, envelope.Source);
        Assert.Equal(MessagingContract.InvoiceTenant, envelope.Tenant);
        Assert.Equal(InvoiceMessageActions.Invoice, envelope.Action);
        Assert.Equal("1.0", envelope.ContentVersion);
        Assert.Null(basePublisher.LastExpiration); // invoice mail must not expire
        Assert.Equal(envelope.Id, result.EnvelopeId);

        // The shared routing-key builder yields the required key from the envelope.
        Assert.Equal("invoice.invoice.invoice", BaseRoutingKey.For(envelope));
        Assert.Equal('5', envelope.Id[14]); // UUID version 5
    }

    [Fact]
    public async Task PublishAsync_DerivesDeterministicId_StableAcrossRuns_UniquePerRecipient()
    {
        var basePublisher = new FakeBaseEventPublisher();
        var sut = CreateSut(basePublisher);

        var first = await sut.PublishAsync(SampleContent("a@example.com"));
        var rerun = await sut.PublishAsync(SampleContent("a@example.com"));
        var other = await sut.PublishAsync(SampleContent("b@example.com"));

        // Re-running a batch reproduces the same id -> email-hub drops the repeat.
        Assert.Equal(first.EnvelopeId, rerun.EnvelopeId);
        // A different recipient is a different message.
        Assert.NotEqual(first.EnvelopeId, other.EnvelopeId);
    }

    [Fact]
    public async Task PublishAsync_WhenBrokerDoesNotConfirm_ReturnsFailed()
    {
        var basePublisher = new FakeBaseEventPublisher { Result = PublishResult.Failed("Timed out waiting for broker confirmation.") };

        var result = await CreateSut(basePublisher).PublishAsync(SampleContent());

        Assert.False(result.Confirmed);
        Assert.Contains("Timed out", result.FailureReason);
    }

    [Fact]
    public void Envelope_Serializes_MachineReadable_ContractShape()
    {
        var envelope = new BaseEventEnvelope<MonthlyInvoiceEmailContent>
        {
            Id = "2b9dc316-98d7-488f-8614-6d275497d0b9",
            Source = "invoice",
            Tenant = "invoice",
            Action = "invoice",
            Timestamp = "2026-07-08T12:44:37.408Z",
            ContentVersion = "1.0",
            Content = SampleContent()
        };

        var json = JsonSerializer.Serialize(envelope, envelope.GetType());
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var content = root.GetProperty("content");
        var rows = content.GetProperty("invoiceRows");

        // Envelope: camelCase keys, no "type" field.
        Assert.Equal("invoice", root.GetProperty("source").GetString());
        Assert.Equal("invoice", root.GetProperty("tenant").GetString());
        Assert.Equal("invoice", root.GetProperty("action").GetString());
        Assert.Equal("1.0", root.GetProperty("contentVersion").GetString());
        Assert.False(root.TryGetProperty("type", out _));

        // Content: ISO/invariant machine-readable values.
        Assert.Equal("EUR", content.GetProperty("currency").GetString());
        Assert.Equal("2026-06", content.GetProperty("invoicePeriod").GetString());
        Assert.Equal("2026-06-06", content.GetProperty("invoiceDate").GetString());
        Assert.Equal("97.87", content.GetProperty("invoiceTotal").GetString());

        // Opaque fields copied verbatim (NOT normalised to ISO).
        Assert.Equal("1D58MKI 06.2026", content.GetProperty("paymentDescription").GetString());
        Assert.Equal("1D58MKI", content.GetProperty("invoiceIdentifier").GetString());
        Assert.Equal("EE621010011679940224", content.GetProperty("paymentIban").GetString());

        // Amounts are strings with trailing zeros; counts are JSON numbers.
        var secondRow = rows[1];
        Assert.Equal(JsonValueKind.String, secondRow.GetProperty("invoiceAmount").ValueKind);
        Assert.Equal("40.00", secondRow.GetProperty("invoiceAmount").GetString());
        Assert.Equal(JsonValueKind.Number, secondRow.GetProperty("fractions").ValueKind);
        Assert.Equal(2, secondRow.GetProperty("fractions").GetInt32());

        // camelCase, not PascalCase.
        Assert.False(content.TryGetProperty("Email", out _));
    }

    private sealed class FakeBaseEventPublisher : IBaseEventPublisher
    {
        public int Calls { get; private set; }
        public string? LastExpiration { get; private set; }
        public IBaseEventEnvelope<MonthlyInvoiceEmailContent>? LastEnvelope { get; private set; }
        public PublishResult Result { get; init; } = PublishResult.Ok();

        public Task<PublishResult> PublishAsync<TContent>(
            IBaseEventEnvelope<TContent> message,
            string? expiration = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastExpiration = expiration;
            LastEnvelope = message as IBaseEventEnvelope<MonthlyInvoiceEmailContent>;
            return Task.FromResult(Result);
        }
    }
}
