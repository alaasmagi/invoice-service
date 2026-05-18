using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceAllocationDtoMapper : IMapper<InvoiceAllocationDto, InvoiceAllocation>
{
    public InvoiceAllocationDto? Map(InvoiceAllocation? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceAllocationDto>? Map(IEnumerable<InvoiceAllocation>? entities)
    {
        throw new NotImplementedException();
    }

    public InvoiceAllocation? Map(InvoiceAllocationDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceAllocation>? Map(IEnumerable<InvoiceAllocationDto>? entities)
    {
        throw new NotImplementedException();
    }
}