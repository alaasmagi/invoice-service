using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class MonthlyStatementLineDtoMapper : IMapper<MonthlyStatementLineDto, MonthlyStatementLine>
{
    public MonthlyStatementLineDto? Map(MonthlyStatementLine? entity)
    {
        return entity == null ? null : new MonthlyStatementLineDto
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

    public IEnumerable<MonthlyStatementLineDto>? Map(IEnumerable<MonthlyStatementLine>? entities)
    {
        return entities?.Select(Map)!;
    }

    public MonthlyStatementLine? Map(MonthlyStatementLineDto? entity)
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

    public IEnumerable<MonthlyStatementLine>? Map(IEnumerable<MonthlyStatementLineDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
