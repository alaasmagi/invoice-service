using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class PublishedInvoiceEmailRepository(AppDbContext context) : IPublishedInvoiceEmailRepository
{
    public Task<int> CountForStatementAsync(Guid monthlyStatementId, Guid userId, CancellationToken ct = default)
    {
        return context.PublishedInvoiceEmails
            .AsNoTracking()
            .CountAsync(e => e.MonthlyStatementId == monthlyStatementId && e.UserId == userId, ct);
    }

    public async Task AddAsync(PublishedInvoiceEmail record, CancellationToken ct = default)
    {
        context.PublishedInvoiceEmails.Add(new PublishedInvoiceEmailEntity
        {
            Id = record.Id,
            UserId = record.UserId,
            MonthlyStatementId = record.MonthlyStatementId,
            Year = record.Year,
            Month = record.Month,
            Attempt = record.Attempt,
            InvoiceIdentifier = record.InvoiceIdentifier,
            Recipient = record.Recipient,
            EnvelopeId = record.EnvelopeId,
            ConfirmedAt = record.ConfirmedAt
        });

        await context.SaveChangesAsync(ct);
    }
}
