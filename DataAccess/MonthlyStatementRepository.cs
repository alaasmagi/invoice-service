using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class MonthlyStatementRepository : BaseRepository<MonthlyStatement, MonthlyStatementEntity, IMapper<MonthlyStatement, MonthlyStatementEntity>>, IMonthlyStatementRepository
{
    private readonly AppDbContext _context;

    public MonthlyStatementRepository(AppDbContext repositoryDbContext, IMapper<MonthlyStatement, MonthlyStatementEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
    }

    public async Task RemoveByIdsAsync(IReadOnlyCollection<Guid> monthlyStatementIds, Guid userId)
    {
        if (monthlyStatementIds.Count == 0)
        {
            return;
        }

        var statements = await _context.MonthlyStatements
            .Where(statement => statement.UserId == userId && monthlyStatementIds.Contains(statement.Id))
            .ToListAsync();

        _context.MonthlyStatements.RemoveRange(statements);
    }

    public async Task SetSendStatusAsync(Guid monthlyStatementId, Guid userId, EMonthlyStatementStatus status, DateTime? sentAt)
    {
        await _context.MonthlyStatements
            .Where(statement => statement.Id == monthlyStatementId && statement.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(statement => statement.Status, status)
                .SetProperty(statement => statement.SentAt, sentAt));
    }
}
