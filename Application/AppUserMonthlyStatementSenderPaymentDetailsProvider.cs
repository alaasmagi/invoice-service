using Contracts.Application;
using Contracts.DataAccess;

namespace Application;

public sealed class AppUserMonthlyStatementSenderPaymentDetailsProvider(IAppUserRepository appUserRepository) : IMonthlyStatementSenderPaymentDetailsProvider
{
    public async Task<MonthlyStatementSenderPaymentDetails?> GetAsync(Guid userId)
    {
        var user = await appUserRepository.GetByIdAsync(userId);
        return user == null
            ? null
            : new MonthlyStatementSenderPaymentDetails
            {
                FullName = user.FullName,
                Locale = user.Locale,
                BankIban = user.BankIban ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(user.Currency) ? "EUR" : user.Currency
            };
    }
}
