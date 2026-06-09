using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceDtoMapper : IMapper<InvoiceDto, Invoice>
{
    public InvoiceDto? Map(Invoice? entity)
    {
        return entity == null ? null : new InvoiceDto
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

    public IEnumerable<InvoiceDto>? Map(IEnumerable<Invoice>? entities)
    {
        return entities?.Select(Map)!;
    }

    public Invoice? Map(InvoiceDto? entity)
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

    public IEnumerable<Invoice>? Map(IEnumerable<InvoiceDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
