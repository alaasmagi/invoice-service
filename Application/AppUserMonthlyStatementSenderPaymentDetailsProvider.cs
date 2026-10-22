using Contracts.Application;
using Contracts.DataAccess;

namespace Application;

public class AppUserMonthlyStatementSenderPaymentDetailsProvider(IAppUserRepository appUserRepository) : IMonthlyStatementSenderPaymentDetailsProvider
{
    public async Task<MonthlyStatementSenderPaymentDetails?> GetAsync(Guid userId)
    {
        var user = await appUserRepository.GetByIdAsync(userId);
        return user == null
            ? null
            : new MonthlyStatementSenderPaymentDetails
            {
                FullName = user.FullName,
                // The app-level language choice wins over the Keycloak locale.
                Locale = string.IsNullOrWhiteSpace(user.Language) ? user.Locale : user.Language,
                BankIban = user.BankIban ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(user.Currency) ? "EUR" : user.Currency
            };
    }
}
