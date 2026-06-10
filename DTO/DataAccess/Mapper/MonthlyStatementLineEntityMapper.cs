using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class MonthlyStatementLineEntityMapper : IMapper<MonthlyStatementLine, MonthlyStatementLineEntity>
{
    public MonthlyStatementLine? Map(MonthlyStatementLineEntity? entity)
    {
        return entity == null ? null : new MonthlyStatementLine
        {
            Id = entity.Id,
            UserId = entity.UserId,
            MonthlyStatementId = entity.MonthlyStatementId,
            InvoiceId = entity.InvoiceId,
            AddressId = entity.AddressId,
            AddressName = entity.AddressName,
            ServiceId = entity.ServiceId,
            ServiceName = entity.ServiceName,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            InvoiceTotal = entity.InvoiceTotal,
            ResidentCount = entity.ResidentCount,
            AllocatedAmount = entity.AllocatedAmount
        };
    }

    public IEnumerable<MonthlyStatementLine>? Map(IEnumerable<MonthlyStatementLineEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public MonthlyStatementLineEntity? Map(MonthlyStatementLine? entity)
    {
        return entity == null ? null : new MonthlyStatementLineEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            MonthlyStatementId = entity.MonthlyStatementId,
            InvoiceId = entity.InvoiceId,
            AddressId = entity.AddressId,
            AddressName = entity.AddressName,
            ServiceId = entity.ServiceId,
            ServiceName = entity.ServiceName,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            InvoiceTotal = entity.InvoiceTotal,
            ResidentCount = entity.ResidentCount,
            AllocatedAmount = entity.AllocatedAmount
        };
    }

    public IEnumerable<MonthlyStatementLineEntity>? Map(IEnumerable<MonthlyStatementLine>? entities)
    {
        return entities?.Select(Map)!;
    }
}
