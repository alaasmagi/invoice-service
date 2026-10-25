using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AllocationRuleRepository : BaseRepository<AllocationRule, AllocationRuleEntity, IMapper<AllocationRule, AllocationRuleEntity>>, IAllocationRuleRepository
{
    private readonly AppDbContext _context;

    public AllocationRuleRepository(AppDbContext repositoryDbContext, IMapper<AllocationRule, AllocationRuleEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
    }

    public async Task<IReadOnlyList<AllocationRule>> GetForUserWithSharesAsync(Guid userId)
    {
        var entities = await _context.AllocationRules
            .AsNoTracking()
            .Include(rule => rule.Shares)
            .Where(rule => rule.UserId == userId)
            .ToListAsync();

        return entities.Select(entity => RepositoryMapper.Map(entity)!).ToList();
    }
}
