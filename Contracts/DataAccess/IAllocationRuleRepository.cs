using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IAllocationRuleRepository : IBaseRepository<AllocationRule>
{
    /// <summary>All of the user's rules with their shares loaded.</summary>
    Task<IReadOnlyList<AllocationRule>> GetForUserWithSharesAsync(Guid userId);
}
