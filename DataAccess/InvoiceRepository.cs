using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class InvoiceRepository : BaseRepository<Invoice, InvoiceEntity, IMapper<Invoice, InvoiceEntity>>, IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext repositoryDbContext, IMapper<Invoice, InvoiceEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
    }

    public async Task<bool> ExistsEquivalentAsync(
        Guid userId,
        Guid serviceId,
        Guid addressId,
        DateOnly invoiceDate,
        DateOnly? periodStart,
        DateOnly? periodEnd,
        decimal totalSum)
    {
        return await _context.Invoices
            .AsNoTracking()
            .AnyAsync(invoice =>
                invoice.UserId == userId
                && invoice.ServiceId == serviceId
                && invoice.AddressId == addressId
                && invoice.InvoiceDate == invoiceDate
                && invoice.PeriodStart == periodStart
                && invoice.PeriodEnd == periodEnd
                && invoice.TotalSum == totalSum);
    }
}
