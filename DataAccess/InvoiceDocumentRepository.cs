using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class InvoiceDocumentRepository : BaseRepository<InvoiceDocument, InvoiceDocumentEntity, IMapper<InvoiceDocument, InvoiceDocumentEntity>>, IInvoiceDocumentRepository
{
    public InvoiceDocumentRepository(AppDbContext repositoryDbContext, IMapper<InvoiceDocument, InvoiceDocumentEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }

    // A document is created and then moved to Processing within one request; the base update attaches a new
    // instance, so the one tracked since creation is detached first.
    public override Task<IMethodResponse<InvoiceDocument>> UpdateAsync(
        Guid id,
        InvoiceDocument entity,
        string? expectedConcurrencyToken = default,
        Guid actor = default)
    {
        var tracked = RepositoryDbSet.Local.FirstOrDefault(document => document.Id == id);
        if (tracked != null)
        {
            RepositoryDbContext.Entry(tracked).State = EntityState.Detached;
        }

        return base.UpdateAsync(id, entity, expectedConcurrencyToken, actor);
    }

    public async Task<InvoiceDocument?> FindBySha256Async(Guid userId, string sha256)
    {
        var entity = await GetQuery(userId).FirstOrDefaultAsync(document => document.Sha256 == sha256);
        return RepositoryMapper.Map(entity);
    }

    public async Task<InvoiceDocument?> GetWithRowsAsync(Guid id, Guid userId = default)
    {
        var entity = await GetQuery(userId)
            .Include(document => document.Rows)
            .FirstOrDefaultAsync(document => document.Id == id);
        return RepositoryMapper.Map(entity);
    }
}
