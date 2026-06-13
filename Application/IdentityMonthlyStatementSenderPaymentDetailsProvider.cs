using Contracts.Application;
using Domain;
using Microsoft.AspNetCore.Identity;

namespace Web;

public sealed class IdentityMonthlyStatementSenderPaymentDetailsProvider(UserManager<AppUser> userManager) : IMonthlyStatementSenderPaymentDetailsProvider
{
    public async Task<MonthlyStatementSenderPaymentDetails?> GetAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user == null
            ? null
            : new MonthlyStatementSenderPaymentDetails
            {
                Fullname = user.Fullname,
                BankIban = user.BankIban ?? string.Empty
            };
    }
}
