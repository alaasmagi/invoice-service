using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceEntityMapper : IMapper<Invoice, InvoiceEntity>
{
    public Invoice? Map(InvoiceEntity? entity)
    {
        return entity == null ? null : new Invoice
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ServiceId = entity.ServiceId,
            AddressId = entity.AddressId,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            TotalSum = entity.TotalSum
        };
    }

    public IEnumerable<Invoice>? Map(IEnumerable<InvoiceEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceEntity? Map(Invoice? entity)
    {
        return entity == null ? null : new InvoiceEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ServiceId = entity.ServiceId,
            AddressId = entity.AddressId,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            TotalSum = entity.TotalSum
        };
    }

    public IEnumerable<InvoiceEntity>? Map(IEnumerable<Invoice>? entities)
    {
        return entities?.Select(Map)!;
    }
}
