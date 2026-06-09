using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class MonthlyStatementDtoMapper : IMapper<MonthlyStatementDto, MonthlyStatement>
{
    public MonthlyStatementDto? Map(MonthlyStatement? entity)
    {
        return entity == null ? null : new MonthlyStatementDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            Year = entity.Year,
            Month = entity.Month,
            TotalSum = entity.TotalSum,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            SentAt = entity.SentAt
        };
    }

    public IEnumerable<MonthlyStatementDto>? Map(IEnumerable<MonthlyStatement>? entities)
    {
        return entities?.Select(Map)!;
    }

    public MonthlyStatement? Map(MonthlyStatementDto? entity)
    {
        return entity == null ? null : new MonthlyStatement
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            Year = entity.Year,
            Month = entity.Month,
            TotalSum = entity.TotalSum,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            SentAt = entity.SentAt
        };
    }

    public IEnumerable<MonthlyStatement>? Map(IEnumerable<MonthlyStatementDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
