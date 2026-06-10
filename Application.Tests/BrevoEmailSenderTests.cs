using System.Net;
using System.Text;
using Application;
using Contracts.Application;
using Xunit;

namespace Application.Tests;

public sealed class BrevoEmailSenderTests
{
    [Fact]
    public async Task SendMonthlyStatementEmailAsyncIncludesSenderPaymentDetails()
    {
        var handler = new CaptureHandler();
        var sender = new BrevoEmailSender(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.test/v3/")
        }, new BrevoEmailOptions
        {
            ApiKey = "test-key",
            SenderEmail = "sender@example.com",
            SenderName = "Invoice Service"
        });

        await sender.SendMonthlyStatementEmailAsync(new MonthlyStatementEmail
        {
            ToEmail = "recipient@example.com",
            ContactName = "Recipient Person",
            SenderBankAccountName = "Aleksander Laasmagi",
            SenderBankIban = "EE621010011679940224",
            Period = "2026-06",
            TotalAmount = 40m,
            Lines = []
        });

        Assert.Contains("Aleksander Laasmagi", handler.CapturedBody);
        Assert.Contains("EE621010011679940224", handler.CapturedBody);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string CapturedBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedBody = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
