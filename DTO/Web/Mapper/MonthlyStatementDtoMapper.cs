using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class MonthlyStatementDtoMapper : IMapper<MonthlyStatementDto, MonthlyStatement>
{
    public MonthlyStatementDto? Map(MonthlyStatement? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MonthlyStatementDto>? Map(IEnumerable<MonthlyStatement>? entities)
    {
        throw new NotImplementedException();
    }

    public MonthlyStatement? Map(MonthlyStatementDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MonthlyStatement>? Map(IEnumerable<MonthlyStatementDto>? entities)
    {
        throw new NotImplementedException();
    }
}