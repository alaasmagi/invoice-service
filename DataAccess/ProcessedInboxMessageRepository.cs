using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess;

public class ProcessedInboxMessageRepository(AppDbContext context) : IProcessedInboxMessageRepository
{
    public async Task<bool> TryInsertAsync(ProcessedInboxMessage record, CancellationToken ct = default)
    {
        var entity = new ProcessedInboxMessageEntity
        {
            Id = record.Id,
            EnvelopeId = record.EnvelopeId,
            Source = record.Source,
            Action = record.Action,
            ProcessedAt = record.ProcessedAt
        };

        context.ProcessedInboxMessages.Add(entity);
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Duplicate delivery — the id is already recorded. Detach so the context is clean.
            context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
