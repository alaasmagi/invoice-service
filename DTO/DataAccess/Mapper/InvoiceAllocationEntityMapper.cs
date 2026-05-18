using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceAllocationEntityMapper : IMapper<InvoiceAllocation, InvoiceAllocationEntity>
{
    public InvoiceAllocation? Map(InvoiceAllocationEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceAllocation>? Map(IEnumerable<InvoiceAllocationEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public InvoiceAllocationEntity? Map(InvoiceAllocation? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceAllocationEntity>? Map(IEnumerable<InvoiceAllocation>? entities)
    {
        throw new NotImplementedException();
    }
}