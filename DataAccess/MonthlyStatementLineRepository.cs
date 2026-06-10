using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class MonthlyStatementLineRepository : BaseRepository<MonthlyStatementLine, MonthlyStatementLineEntity, IMapper<MonthlyStatementLine, MonthlyStatementLineEntity>>, IMonthlyStatementLineRepository
{
    private readonly AppDbContext _context;

    public MonthlyStatementLineRepository(AppDbContext repositoryDbContext, IMapper<MonthlyStatementLine, MonthlyStatementLineEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
    }

    public async Task RemoveByStatementIdsAsync(IReadOnlyCollection<Guid> monthlyStatementIds, Guid userId)
    {
        if (monthlyStatementIds.Count == 0)
        {
            return;
        }

        var existingLines = await _context.MonthlyStatementLines
            .Where(line => line.UserId == userId && monthlyStatementIds.Contains(line.MonthlyStatementId))
            .ToListAsync();

        _context.MonthlyStatementLines.RemoveRange(existingLines);
    }
}
