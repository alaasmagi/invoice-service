using Contracts.Application;
using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

public class InvoicesController(AppDbContext context, ICreateInvoiceService createInvoiceService, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index(string? period, Guid? addressId, Guid? serviceId)
    {
        var userId = CurrentUserId();
        var query = context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .Where(e => e.UserId == userId);

        var hasPeriod = BillingPeriod.TryParse(period, out var billingPeriod);
        if (hasPeriod) query = query.InPeriod(billingPeriod);
        if (addressId != null) query = query.Where(e => e.AddressId == addressId);
        if (serviceId != null) query = query.Where(e => e.ServiceId == serviceId);

        var invoices = await query.OrderByDescending(e => e.InvoiceDate).ThenBy(e => e.Address.Name).ToListAsync();
        var invoiceMonths = await context.Invoices.Where(e => e.UserId == userId)
            .Select(e => new { e.InvoiceDate.Year, e.InvoiceDate.Month })
            .Distinct()
            .ToListAsync();

        return View(new InvoicesIndexViewModel
        {
            Period = hasPeriod ? billingPeriod.Key : null,
            AddressId = addressId,
            ServiceId = serviceId,
            PeriodOptions = BillingPeriod.Options(invoiceMonths.Select(m => new BillingPeriod(m.Year, m.Month))),
            AddressOptions = await AddressOptions(userId, addressId),
            ServiceOptions = await ServiceOptions(userId, serviceId),
            Invoices = invoices.Select(mapper.ToDto).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();

        var residents = await context.AddressContacts.Include(e => e.Contact)
            .Where(e => e.UserId == userId && e.AddressId == entity.AddressId)
            .ToListAsync();
        var active = residents
            .Where(e => e.IsActive(entity.InvoiceDate))
            .GroupBy(e => e.ContactId)
            .Select(g => g.First())
            .OrderBy(e => e.ContactId)
            .ToList();

        var lines = await context.MonthlyStatementLines.Include(e => e.MonthlyStatement).ThenInclude(e => e.Contact)
            .Where(e => e.UserId == userId && e.InvoiceId == entity.Id)
            .OrderByDescending(e => e.MonthlyStatement.Year).ThenByDescending(e => e.MonthlyStatement.Month)
            .ToListAsync();

        return View(new InvoiceDetailsViewModel
        {
            Invoice = mapper.ToDto(entity),
            Split = SplitEvenly(entity.TotalSum, active.Select(e => (e.ContactId, e.Contact.FullName)).ToList()),
            StatementLines = lines.Select(mapper.ToDto).ToList()
        });
    }

    public async Task<IActionResult> Create(Guid? addressId, Guid? serviceId, DateOnly? invoiceDate, DateOnly? periodStart, DateOnly? periodEnd)
    {
        var userId = CurrentUserId();
        await PopulateForm(userId);
        return View(new InvoiceDto
        {
            AddressId = addressId ?? Guid.Empty,
            ServiceId = serviceId ?? Guid.Empty,
            InvoiceDate = invoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ServiceId,AddressId,InvoiceDate,PeriodStart,PeriodEnd,TotalSum")] InvoiceDto dto, string? next)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Address), nameof(dto.Service), nameof(dto.Allocations));
        ValidatePeriod(dto);
        if (!await OwnsAddressAndService(userId, dto.AddressId, dto.ServiceId)) ModelState.AddModelError(string.Empty, "Selected address or service was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateForm(userId);
            return View(dto);
        }

        var invoice = mapper.ToDomain(dto);
        invoice.Id = Guid.NewGuid();
        invoice.UserId = userId;
        await createInvoiceService.CreateAsync(invoice, userId);

        var addressName = await context.Addresses.Where(e => e.Id == dto.AddressId).Select(e => e.Name).FirstAsync();
        if (next == "another")
        {
            Flash($"Invoice saved for {addressName} ({Fmt.Money(dto.TotalSum)}). Add the next one.");
            return RedirectToAction(nameof(Create), new
            {
                addressId = dto.AddressId,
                invoiceDate = dto.InvoiceDate.ToString("yyyy-MM-dd"),
                periodStart = dto.PeriodStart?.ToString("yyyy-MM-dd"),
                periodEnd = dto.PeriodEnd?.ToString("yyyy-MM-dd")
            });
        }

        Flash($"Invoice saved for {addressName} ({Fmt.Money(dto.TotalSum)}).");
        return RedirectToAction(nameof(Index), new { period = BillingPeriod.From(dto.InvoiceDate).Key });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Invoices.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateForm(userId, entity.Id);
        return View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,ServiceId,AddressId,InvoiceDate,PeriodStart,PeriodEnd,TotalSum")] InvoiceDto dto)
    {
        if (id != dto.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Address), nameof(dto.Service), nameof(dto.Allocations));
        ValidatePeriod(dto);
        if (!await OwnsAddressAndService(userId, dto.AddressId, dto.ServiceId)) ModelState.AddModelError(string.Empty, "Selected address or service was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateForm(userId, dto.Id);
            return View(dto);
        }
        var existing = await context.Invoices.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();

        var inStatements = await context.MonthlyStatementLines.AnyAsync(e => e.UserId == userId && e.InvoiceId == id);
        if (inStatements) Flash("Invoice updated. Regenerate the statements that include it so they show the new figures.", "warning");
        else Flash("Invoice updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(await BuildDeleteModel(entity.Id, userId, mapper.ToDto(entity)));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.Invoices.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            if (await context.MonthlyStatementLines.AnyAsync(e => e.UserId == userId && e.InvoiceId == id))
            {
                Flash("This invoice is part of a monthly statement and can't be deleted.", "danger");
                return RedirectToAction(nameof(Delete), new { id });
            }
            context.Invoices.Remove(entity);
            await context.SaveChangesAsync();
            Flash("Invoice deleted.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<DeleteViewModel<InvoiceDto>> BuildDeleteModel(Guid invoiceId, Guid userId, InvoiceDto dto)
    {
        var statements = await context.MonthlyStatementLines
            .Where(e => e.UserId == userId && e.InvoiceId == invoiceId)
            .Select(e => new { e.MonthlyStatement.Contact.FullName, e.MonthlyStatement.Year, e.MonthlyStatement.Month, e.MonthlyStatement.Status })
            .ToListAsync();
        var allocationCount = await context.InvoiceAllocations.CountAsync(e => e.UserId == userId && e.InvoiceId == invoiceId);

        return new DeleteViewModel<InvoiceDto>
        {
            Item = dto,
            Blockers = statements
                .Select(s => $"Included in the {new BillingPeriod(s.Year, s.Month).Label} statement for {s.FullName} ({s.Status}). Delete that statement first.")
                .ToList(),
            Consequences = allocationCount > 0 ? [$"{allocationCount} allocation record(s) for this invoice will also be deleted."] : []
        };
    }

    private void ValidatePeriod(InvoiceDto dto)
    {
        if (dto.PeriodStart != null && dto.PeriodEnd != null && dto.PeriodEnd < dto.PeriodStart)
        {
            ModelState.AddModelError(nameof(dto.PeriodEnd), "Period end can't be before period start.");
        }
    }

    private async Task PopulateForm(Guid userId, Guid? invoiceId = null)
    {
        ViewData["AddressId"] = new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
        ViewData["ServiceId"] = new SelectList(await context.Services.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");

        var statements = invoiceId == null
            ? []
            : await context.MonthlyStatements.Include(e => e.Contact)
                .Where(e => e.UserId == userId && e.Lines.Any(line => line.InvoiceId == invoiceId))
                .ToListAsync();

        ViewData["Form"] = new InvoiceFormViewModel
        {
            HasAddresses = await context.Addresses.AnyAsync(e => e.UserId == userId),
            HasServices = await context.Services.AnyAsync(e => e.UserId == userId),
            IncludedInStatements = statements.Select(mapper.ToDto).ToList()
        };
    }

    private async Task<IEnumerable<SelectListItem>> AddressOptions(Guid userId, Guid? selected) =>
        new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name", selected);

    private async Task<IEnumerable<SelectListItem>> ServiceOptions(Guid userId, Guid? selected) =>
        new SelectList(await context.Services.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name", selected);

    private async Task<bool> OwnsAddressAndService(Guid userId, Guid addressId, Guid serviceId)
    {
        return await context.Addresses.AnyAsync(e => e.Id == addressId && e.UserId == userId)
               && await context.Services.AnyAsync(e => e.Id == serviceId && e.UserId == userId);
    }

    /// <summary>Mirrors the equal split used by monthly statement generation, for a preview on the details page.</summary>
    private static IReadOnlyList<ShareRow> SplitEvenly(decimal total, IReadOnlyList<(Guid ContactId, string Name)> contacts)
    {
        if (contacts.Count == 0) return [];
        var baseAmount = decimal.Round(total / contacts.Count, 2, MidpointRounding.AwayFromZero);
        var rows = new List<ShareRow>();
        var allocated = 0m;
        for (var i = 0; i < contacts.Count; i++)
        {
            var amount = i == contacts.Count - 1 ? total - allocated : baseAmount;
            allocated += amount;
            rows.Add(new ShareRow(contacts[i].ContactId, contacts[i].Name, amount));
        }
        return rows.OrderBy(r => r.ContactName).ToList();
    }
}
