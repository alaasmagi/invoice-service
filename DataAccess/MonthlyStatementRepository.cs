using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class MonthlyStatementRepository : BaseRepository<MonthlyStatement, MonthlyStatementEntity, IMapper<MonthlyStatement, MonthlyStatementEntity>>, IMonthlyStatementRepository
{
    public MonthlyStatementRepository(DbContext repositoryDbContext, IMapper<MonthlyStatement, MonthlyStatementEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}