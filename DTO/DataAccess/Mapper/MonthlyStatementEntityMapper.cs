using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class MonthlyStatementEntityMapper : IMapper<MonthlyStatement, MonthlyStatementEntity>
{
    public MonthlyStatement? Map(MonthlyStatementEntity? entity)
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

    public IEnumerable<MonthlyStatement>? Map(IEnumerable<MonthlyStatementEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public MonthlyStatementEntity? Map(MonthlyStatement? entity)
    {
        return entity == null ? null : new MonthlyStatementEntity
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

    public IEnumerable<MonthlyStatementEntity>? Map(IEnumerable<MonthlyStatement>? entities)
    {
        return entities?.Select(Map)!;
    }
}
