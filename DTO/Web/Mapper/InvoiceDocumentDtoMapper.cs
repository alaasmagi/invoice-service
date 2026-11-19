using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceDocumentDtoMapper : IMapper<InvoiceDocumentDto, InvoiceDocument>
{
    public InvoiceDocumentDto? Map(InvoiceDocument? entity)
    {
        return entity == null ? null : new InvoiceDocumentDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            Status = entity.Status.ToString()
        };
    }

    public IEnumerable<InvoiceDocumentDto>? Map(IEnumerable<InvoiceDocument>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceDocument? Map(InvoiceDocumentDto? entity)
    {
        return entity == null ? null : new InvoiceDocument
        {
            Id = entity.Id,
            FileName = entity.FileName,
            Status = Enum.Parse<EInvoiceDocumentStatus>(entity.Status)
        };
    }

    public IEnumerable<InvoiceDocument>? Map(IEnumerable<InvoiceDocumentDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
