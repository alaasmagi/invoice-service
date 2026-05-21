using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class InvoiceAllocationRepository : BaseRepository<InvoiceAllocation, InvoiceAllocationEntity, IMapper<InvoiceAllocation, InvoiceAllocationEntity>>, IInvoiceAllocationRepository
{
    public InvoiceAllocationRepository(DbContext repositoryDbContext, IMapper<InvoiceAllocation, InvoiceAllocationEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}