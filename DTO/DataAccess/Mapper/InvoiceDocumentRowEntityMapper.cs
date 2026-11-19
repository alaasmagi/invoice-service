using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceDocumentRowEntityMapper : IMapper<InvoiceDocumentRow, InvoiceDocumentRowEntity>
{
    public InvoiceDocumentRow? Map(InvoiceDocumentRowEntity? entity)
    {
        return entity == null ? null : new InvoiceDocumentRow
        {
            Id = entity.Id,
            InvoiceDocumentId = entity.InvoiceDocumentId,
            Position = entity.Position,
            AddressText = entity.AddressText,
            Amount = entity.Amount,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            Outcome = entity.Outcome,
            Reason = entity.Reason,
            InvoiceId = entity.InvoiceId
        };
    }

    public IEnumerable<InvoiceDocumentRow>? Map(IEnumerable<InvoiceDocumentRowEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceDocumentRowEntity? Map(InvoiceDocumentRow? entity)
    {
        return entity == null ? null : new InvoiceDocumentRowEntity
        {
            Id = entity.Id,
            InvoiceDocumentId = entity.InvoiceDocumentId,
            Position = entity.Position,
            AddressText = entity.AddressText,
            Amount = entity.Amount,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            Outcome = entity.Outcome,
            Reason = entity.Reason,
            InvoiceId = entity.InvoiceId
        };
    }

    public IEnumerable<InvoiceDocumentRowEntity>? Map(IEnumerable<InvoiceDocumentRow>? entities)
    {
        return entities?.Select(Map)!;
    }
}
