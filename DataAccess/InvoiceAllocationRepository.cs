using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class InvoiceAllocationRepository : BaseRepository<InvoiceAllocation, InvoiceAllocationEntity, IMapper<InvoiceAllocation, InvoiceAllocationEntity>>, IInvoiceAllocationRepository
{
    private readonly AppDbContext _context;

    public InvoiceAllocationRepository(AppDbContext repositoryDbContext, IMapper<InvoiceAllocation, InvoiceAllocationEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
    }

    public async Task<IReadOnlyList<InvoiceAllocation>> GetOverridesByInvoiceIdsAsync(IReadOnlyCollection<Guid> invoiceIds, Guid userId)
    {
        if (invoiceIds.Count == 0)
        {
            return [];
        }

        var entities = await _context.InvoiceAllocations
            .AsNoTracking()
            .Where(allocation => allocation.UserId == userId && allocation.IsOverride && invoiceIds.Contains(allocation.InvoiceId))
            .ToListAsync();

        return entities.Select(entity => RepositoryMapper.Map(entity)!).ToList();
    }
}
