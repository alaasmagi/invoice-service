using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Localization;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

public class AddressesController(AppDbContext context, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var addresses = await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync();
        var residents = await context.AddressContacts.Where(e => e.UserId == userId).ToListAsync();
        ViewData["ResidentCounts"] = addresses.ToDictionary(
            a => a.Id,
            a => residents.Where(r => r.AddressId == a.Id && r.IsActive(today)).Select(r => r.ContactId).Distinct().Count());
        return View(addresses.Select(mapper.ToDto).ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Addresses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();

        var residents = await context.AddressContacts.Include(e => e.Contact)
            .Where(e => e.UserId == userId && e.AddressId == entity.Id)
            .OrderBy(e => e.EndDate != null).ThenByDescending(e => e.StartDate)
            .ToListAsync();
        var invoices = context.Invoices.Include(e => e.Service).Where(e => e.UserId == userId && e.AddressId == entity.Id);

        return View(new AddressDetailsViewModel
        {
            Address = mapper.ToDto(entity),
            Residents = residents.Select(mapper.ToDto).ToList(),
            RecentInvoices = (await invoices.OrderByDescending(e => e.InvoiceDate).Take(10).ToListAsync()).Select(mapper.ToDto).ToList(),
            InvoiceCount = await invoices.CountAsync()
        });
    }

    public IActionResult Create(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new AddressDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,FullAddress")] AddressDto dto, string? returnUrl)
    {
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.AddressContacts), nameof(dto.Invoices), nameof(dto.MonthlyStatements));
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, CurrentUserId());
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash(T["Address \"{0}\" added. Next, add the people who live there.", entity.Name]);
        return RedirectToLocalOr(returnUrl, nameof(Details), new { id = entity.Id });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Addresses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,FullAddress")] AddressDto dto)
    {
        if (id != dto.Id) return NotFound();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.AddressContacts), nameof(dto.Invoices), nameof(dto.MonthlyStatements));
        if (!ModelState.IsValid) return View(dto);
        var userId = CurrentUserId();
        var existing = await context.Addresses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash(T["Address updated."]);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Addresses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(await BuildDeleteModel(entity.Id, userId, mapper.ToDto(entity)));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.Addresses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            if (await context.MonthlyStatementLines.AnyAsync(e => e.UserId == userId && e.AddressId == id))
            {
                Flash(T["This address appears on monthly statements and can't be deleted."], "danger");
                return RedirectToAction(nameof(Delete), new { id });
            }
            context.Addresses.Remove(entity);
            await context.SaveChangesAsync();
            Flash(T["Address \"{0}\" deleted.", entity.Name]);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<DeleteViewModel<AddressDto>> BuildDeleteModel(Guid addressId, Guid userId, AddressDto dto)
    {
        var statementCount = await context.MonthlyStatementLines
            .Where(e => e.UserId == userId && e.AddressId == addressId)
            .Select(e => e.MonthlyStatementId).Distinct().CountAsync();
        var invoiceCount = await context.Invoices.CountAsync(e => e.UserId == userId && e.AddressId == addressId);
        var residentCount = await context.AddressContacts.CountAsync(e => e.UserId == userId && e.AddressId == addressId);

        var consequences = new List<string>();
        if (invoiceCount > 0) consequences.Add(T.Plural(invoiceCount, "{0} invoice for this address will also be deleted.", "{0} invoices for this address will also be deleted."));
        if (residentCount > 0) consequences.Add(T.Plural(residentCount, "{0} resident record will also be deleted. The contact itself stays.", "{0} resident records will also be deleted. The contacts themselves stay."));

        return new DeleteViewModel<AddressDto>
        {
            Item = dto,
            Blockers = statementCount > 0
                ? [T.Plural(statementCount, "Invoices for this address are included in {0} monthly statement. Delete that statement first, or keep the address for history.", "Invoices for this address are included in {0} monthly statements. Delete those statements first, or keep the address for history.")]
                : [],
            Consequences = consequences
        };
    }
}
