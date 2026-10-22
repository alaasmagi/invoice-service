using Application;
using Contracts.DataAccess;
using Domain;
using Moq;
using Xunit;

namespace Tests;

public class SenderPaymentDetailsProviderTests
{
    private static async Task<string> EmailLocaleFor(string locale, string? language)
    {
        var userId = Guid.NewGuid();
        var repository = new Mock<IAppUserRepository>();
        repository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = userId, FullName = "Payee", Locale = locale, Language = language, Currency = "EUR" });

        var details = await new AppUserMonthlyStatementSenderPaymentDetailsProvider(repository.Object).GetAsync(userId);
        return details!.Locale;
    }

    [Fact]
    public async Task Locale_WithoutAppLanguage_FollowsKeycloak()
    {
        Assert.Equal("et", await EmailLocaleFor("et", null));
    }

    [Fact]
    public async Task Locale_WithAppLanguage_OverridesKeycloak()
    {
        Assert.Equal("en", await EmailLocaleFor("et", "en"));
    }
}
