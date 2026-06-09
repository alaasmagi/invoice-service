using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceAllocationDtoMapper : IMapper<InvoiceAllocationDto, InvoiceAllocation>
{
    public InvoiceAllocationDto? Map(InvoiceAllocation? entity)
    {
        return entity == null ? null : new InvoiceAllocationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            InvoiceId = entity.InvoiceId,
            ContactId = entity.ContactId,
            AllocatedSum = entity.AllocatedSum,
            CreatedAt = entity.CreatedAt
        };
    }

    public IEnumerable<InvoiceAllocationDto>? Map(IEnumerable<InvoiceAllocation>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceAllocation? Map(InvoiceAllocationDto? entity)
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

    public IEnumerable<InvoiceAllocation>? Map(IEnumerable<InvoiceAllocationDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
