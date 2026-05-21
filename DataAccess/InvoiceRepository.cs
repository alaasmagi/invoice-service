using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class InvoiceRepository : BaseRepository<Invoice, InvoiceEntity, IMapper<Invoice, InvoiceEntity>>, IInvoiceRepository
{
    public InvoiceRepository(DbContext repositoryDbContext, IMapper<Invoice, InvoiceEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}