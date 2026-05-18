using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceEntityMapper : IMapper<Invoice, InvoiceEntity>
{
    public Invoice? Map(InvoiceEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Invoice>? Map(IEnumerable<InvoiceEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public InvoiceEntity? Map(Invoice? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceEntity>? Map(IEnumerable<Invoice>? entities)
    {
        throw new NotImplementedException();
    }
}