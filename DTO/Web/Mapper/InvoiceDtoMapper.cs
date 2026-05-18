using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceDtoMapper : IMapper<InvoiceDto, Invoice>
{
    public InvoiceDto? Map(Invoice? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<InvoiceDto>? Map(IEnumerable<Invoice>? entities)
    {
        throw new NotImplementedException();
    }

    public Invoice? Map(InvoiceDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Invoice>? Map(IEnumerable<InvoiceDto>? entities)
    {
        throw new NotImplementedException();
    }
}