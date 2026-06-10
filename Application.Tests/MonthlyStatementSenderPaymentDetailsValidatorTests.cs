using Application;
using Contracts.Application;
using Xunit;

namespace Application.Tests;

public sealed class MonthlyStatementSenderPaymentDetailsValidatorTests
{
    [Fact]
    public void EnsureCanSendRejectsMissingSenderProfile()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MonthlyStatementSenderPaymentDetailsValidator.EnsureCanSend(null));

        Assert.Contains("Sender profile", ex.Message);
    }

    [Fact]
    public void EnsureCanSendRejectsMissingFullname()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MonthlyStatementSenderPaymentDetailsValidator.EnsureCanSend(new MonthlyStatementSenderPaymentDetails
        {
            Fullname = " ",
            BankIban = "EE621010011679940224"
        }));

        Assert.Contains("full name", ex.Message);
    }

    [Fact]
    public void EnsureCanSendRejectsMissingBankIban()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MonthlyStatementSenderPaymentDetailsValidator.EnsureCanSend(new MonthlyStatementSenderPaymentDetails
        {
            Fullname = "Aleksander Laasmagi",
            BankIban = " "
        }));

        Assert.Contains("bank IBAN", ex.Message);
    }

    [Fact]
    public void EnsureCanSendAcceptsCompleteDetails()
    {
        MonthlyStatementSenderPaymentDetailsValidator.EnsureCanSend(new MonthlyStatementSenderPaymentDetails
        {
            Fullname = "Aleksander Laasmagi",
            BankIban = "EE621010011679940224"
        });
    }
}
