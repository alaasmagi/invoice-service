using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DataAccess;

public class InvoiceDocumentRowRepository : BaseRepository<InvoiceDocumentRow, InvoiceDocumentRowEntity, IMapper<InvoiceDocumentRow, InvoiceDocumentRowEntity>>, IInvoiceDocumentRowRepository
{
    public InvoiceDocumentRowRepository(AppDbContext repositoryDbContext, IMapper<InvoiceDocumentRow, InvoiceDocumentRowEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}
