using Contracts.Application;
using Contracts.DataAccess;

namespace Web;

public sealed class IdentityMonthlyStatementSenderPaymentDetailsProvider(IAppUserRepository appUserRepository) : IMonthlyStatementSenderPaymentDetailsProvider
{
    public async Task<MonthlyStatementSenderPaymentDetails?> GetAsync(Guid userId)
    {
        var user = await appUserRepository.GetByIdAsync(userId);
        return user == null
            ? null
            : new MonthlyStatementSenderPaymentDetails
            {
                Fullname = user.Fullname,
                BankIban = user.BankIban ?? string.Empty
            };
    }
}
