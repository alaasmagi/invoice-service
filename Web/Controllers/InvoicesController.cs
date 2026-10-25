using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Localization;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

public class InvoicesController(AppDbContext context, ICreateInvoiceService createInvoiceService, IInvoiceSplitCalculator splitCalculator, WebDtoMapper mapper) : UserScopedControllerBase
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

        var split = await PreviewSplit(entity, userId);

        var lines = await context.MonthlyStatementLines.Include(e => e.MonthlyStatement).ThenInclude(e => e.Contact)
            .Where(e => e.UserId == userId && e.InvoiceId == entity.Id)
            .OrderByDescending(e => e.MonthlyStatement.Year).ThenByDescending(e => e.MonthlyStatement.Month)
            .ToListAsync();

        return View(new InvoiceDetailsViewModel
        {
            Invoice = mapper.ToDto(entity),
            Split = split.Rows,
            SplitMethod = split.Result.Method,
            SplitRuleId = split.Result.RuleId,
            SplitWarnings = split.Warnings,
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
        if (!await OwnsAddressAndService(userId, dto.AddressId, dto.ServiceId)) ModelState.AddModelError(string.Empty, T["Selected address or service was not found."]);
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
            Flash(T["Invoice saved for {0} ({1}). Add the next one.", addressName, Fmt.Money(dto.TotalSum)]);
            return RedirectToAction(nameof(Create), new
            {
                addressId = dto.AddressId,
                invoiceDate = dto.InvoiceDate.ToString("yyyy-MM-dd"),
                periodStart = dto.PeriodStart?.ToString("yyyy-MM-dd"),
                periodEnd = dto.PeriodEnd?.ToString("yyyy-MM-dd")
            });
        }

        Flash(T["Invoice saved for {0} ({1}).", addressName, Fmt.Money(dto.TotalSum)]);
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
        if (!await OwnsAddressAndService(userId, dto.AddressId, dto.ServiceId)) ModelState.AddModelError(string.Empty, T["Selected address or service was not found."]);
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
        if (inStatements) Flash(T["Invoice updated. Regenerate the statements that include it so they show the new figures."], "warning");
        else Flash(T["Invoice updated."]);
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
                Flash(T["This invoice is part of a monthly statement and can't be deleted."], "danger");
                return RedirectToAction(nameof(Delete), new { id });
            }
            context.Invoices.Remove(entity);
            await context.SaveChangesAsync();
            Flash(T["Invoice deleted."]);
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
                .Select(s => T["Included in the {0} statement for {1} ({2}). Delete that statement first.", new BillingPeriod(s.Year, s.Month).Label, s.FullName, Fmt.StatusLabel(s.Status)].Value)
                .ToList(),
            Consequences = allocationCount > 0 ? [T.Plural(allocationCount, "{0} allocation record for this invoice will also be deleted.", "{0} allocation records for this invoice will also be deleted.")] : []
        };
    }

    private void ValidatePeriod(InvoiceDto dto)
    {
        if (dto.PeriodStart != null && dto.PeriodEnd != null && dto.PeriodEnd < dto.PeriodStart)
        {
            ModelState.AddModelError(nameof(dto.PeriodEnd), T["Period end can't be before period start."]);
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

    /// <summary>How the next statement generation will split this invoice, using the same calculator.</summary>
    private async Task<(InvoiceSplitResult Result, IReadOnlyList<ShareRow> Rows, IReadOnlyList<AllocationWarning> Warnings)> PreviewSplit(InvoiceEntity entity, Guid userId)
    {
        var activeIds = await context.AddressContacts
            .Where(e => e.UserId == userId && e.AddressId == entity.AddressId
                        && e.StartDate <= entity.InvoiceDate && (e.EndDate == null || e.EndDate >= entity.InvoiceDate))
            .Select(e => e.ContactId)
            .Distinct()
            .ToListAsync();
        var manual = (await context.InvoiceAllocations.Where(e => e.UserId == userId && e.InvoiceId == entity.Id && e.IsOverride).ToListAsync())
            .Select(e => new InvoiceAllocation { Id = e.Id, UserId = e.UserId, InvoiceId = e.InvoiceId, ContactId = e.ContactId, AllocatedSum = e.AllocatedSum, IsOverride = true })
            .ToList();
        var rules = (await context.AllocationRules.Include(e => e.Shares)
                .Where(e => e.UserId == userId && e.AddressId == entity.AddressId)
                .ToListAsync())
            .Select(mapper.ToDomain)
            .ToList();

        var result = splitCalculator.Calculate(new InvoiceSplitInput
        {
            Invoice = mapper.ToDomain(entity),
            ManualAllocations = manual,
            Rules = rules,
            ActiveResidentIds = activeIds.OrderBy(id => id).ToList()
        });

        var contactIds = result.Shares.Select(e => e.ContactId)
            .Concat(result.Warnings.Where(e => e.ContactId != null).Select(e => e.ContactId!.Value))
            .Distinct()
            .ToList();
        var names = await context.Contacts.Where(e => e.UserId == userId && contactIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName);
        var rows = result.Shares
            .Select(e => new ShareRow
            {
                ContactId = e.ContactId,
                ContactName = names.GetValueOrDefault(e.ContactId, "—"),
                Amount = e.Amount,
                SplitLabel = Fmt.SplitLabel(result.Method, e.ShareValue, result.ParticipantCount)
            })
            .OrderBy(e => e.ContactName)
            .ToList();
        foreach (var warning in result.Warnings)
        {
            if (warning.ContactId is { } contactId) warning.ContactName = names.GetValueOrDefault(contactId);
        }
        return (result, rows, result.Warnings);
    }
}
