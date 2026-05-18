using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class MonthlyStatementEntityMapper : IMapper<MonthlyStatement, MonthlyStatementEntity>
{
    public MonthlyStatement? Map(MonthlyStatementEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MonthlyStatement>? Map(IEnumerable<MonthlyStatementEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public MonthlyStatementEntity? Map(MonthlyStatement? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MonthlyStatementEntity>? Map(IEnumerable<MonthlyStatement>? entities)
    {
        throw new NotImplementedException();
    }
}