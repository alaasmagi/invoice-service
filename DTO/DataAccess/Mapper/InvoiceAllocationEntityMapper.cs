using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceAllocationEntityMapper : IMapper<InvoiceAllocation, InvoiceAllocationEntity>
{
    public InvoiceAllocation? Map(InvoiceAllocationEntity? entity)
    {
        return entity == null ? null : new InvoiceAllocation
        {
            Id = entity.Id,
            UserId = entity.UserId,
            InvoiceId = entity.InvoiceId,
            ContactId = entity.ContactId,
            AllocatedSum = entity.AllocatedSum,
            CreatedAt = entity.CreatedAt
        };
    }

    public IEnumerable<InvoiceAllocation>? Map(IEnumerable<InvoiceAllocationEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceAllocationEntity? Map(InvoiceAllocation? entity)
    {
        return entity == null ? null : new InvoiceAllocationEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            InvoiceId = entity.InvoiceId,
            ContactId = entity.ContactId,
            AllocatedSum = entity.AllocatedSum,
            CreatedAt = entity.CreatedAt
        };
    }

    public IEnumerable<InvoiceAllocationEntity>? Map(IEnumerable<InvoiceAllocation>? entities)
    {
        return entities?.Select(Map)!;
    }
}
