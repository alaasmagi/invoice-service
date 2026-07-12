using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;
using Infrastructure.Messaging;
using System.Text.Json;
using Xunit;

namespace Tests;

public sealed class RabbitMqMonthlyInvoiceEmailPublisherTests
{
    [Fact]
    public async Task PublishAsync_UsesEnvelopeTypeAsRoutingTopic()
    {
        var basePublisher = new CapturingBaseEventPublisher();
        var options = new InvoiceMessagingOptions
        {
            Queue = "invoice-service",
            IdentitySource = "identity.invoice-service",
            EmailSource = "invoice"
        };
        var content = new MonthlyInvoiceEmailContent
        {
            Email = "recipient@example.test",
            FullName = "Recipient",
            InvoiceIdentifier = "ABC1234",
            InvoicePeriod = "07.2026",
            InvoiceDate = "12.07.2026",
            InvoiceDueDate = "26.07.2026",
            InvoiceTotal = "300,00",
            PaymentFullName = "Sender",
            PaymentIban = "EE382200221020145685",
            PaymentDescription = "ABC1234 07.2026"
        };
        var publisher = new RabbitMqMonthlyInvoiceEmailPublisher(basePublisher, options);

        await publisher.PublishAsync(content, CancellationToken.None);

        Assert.Equal(DefaultMessageTypes.Email, basePublisher.Topic);
        var envelope = Assert.IsAssignableFrom<IBaseEventEnvelope<MonthlyInvoiceEmailContent>>(basePublisher.Message);
        Assert.Equal(envelope.Type, basePublisher.Topic);
        Assert.Equal(DefaultMessageTypes.Email, envelope.Type);
        Assert.Equal("invoice", envelope.Source);
        Assert.Equal(InvoiceMessageActions.MonthlyInvoice, envelope.Action);
        Assert.Same(content, envelope.Content);
    }

    [Fact]
    public void MonthlyInvoiceEmailContent_SerializesToEmailCommandContract()
    {
        var envelope = new BaseEventEnvelope<MonthlyInvoiceEmailContent>
        {
            Type = DefaultMessageTypes.Email,
            Source = "invoice",
            Action = InvoiceMessageActions.MonthlyInvoice,
            Timestamp = new DateTime(2026, 7, 8, 12, 44, 37, DateTimeKind.Utc),
            Content = new MonthlyInvoiceEmailContent
            {
                Email = "user@example.com",
                FullName = "testFullName",
                Locale = "et",
                InvoiceIdentifier = "1D58MKI",
                InvoicePeriod = "06.2026",
                InvoiceDate = "06.06.2026",
                InvoiceDueDate = "20.06.2026",
                InvoiceTotal = "97,87",
                Currency = "€",
                InvoiceRows =
                [
                    new MonthlyInvoiceEmailRowContent
                    {
                        Address = "Leesi - Tiiu",
                        Service = "Enefit elektriteenus",
                        PeriodStart = "01.05.2026",
                        PeriodEnd = "31.05.2026",
                        InvoiceAmount = "75,75",
                        Fractions = 1,
                        InvoiceFraction = "75,75"
                    }
                ],
                PaymentFullName = "Aleksander Laasmägi",
                PaymentIban = "EE621010011679940224",
                PaymentDescription = "1D58MKI 06.2026"
            }
        };

        var json = JsonSerializer.Serialize(envelope);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var content = root.GetProperty("content");
        var row = content.GetProperty("invoiceRows")[0];

        Assert.Equal("email", root.GetProperty("type").GetString());
        Assert.Equal("invoice", root.GetProperty("source").GetString());
        Assert.Equal("monthly.invoice", root.GetProperty("action").GetString());
        Assert.Equal("user@example.com", content.GetProperty("email").GetString());
        Assert.Equal("testFullName", content.GetProperty("fullName").GetString());
        Assert.Equal("EE621010011679940224", content.GetProperty("paymentIban").GetString());
        Assert.Equal("Leesi - Tiiu", row.GetProperty("address").GetString());
        Assert.Equal("75,75", row.GetProperty("invoiceFraction").GetString());
        Assert.False(content.TryGetProperty("Email", out _));
        Assert.False(content.TryGetProperty("InvoiceRows", out _));
        Assert.False(row.TryGetProperty("InvoiceFraction", out _));
    }

    private sealed class CapturingBaseEventPublisher : IBaseEventPublisher
    {
        public string? Topic { get; private set; }
        public object? Message { get; private set; }

        public Task PublishAsync<TContent>(
            string topic,
            IBaseEventEnvelope<TContent> message,
            CancellationToken cancellationToken = default)
        {
            Topic = topic;
            Message = message;
            return Task.CompletedTask;
        }
    }
}
