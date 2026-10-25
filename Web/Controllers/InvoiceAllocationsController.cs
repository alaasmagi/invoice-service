using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Mapping;

namespace Web.Controllers;

public class InvoiceAllocationsController(AppDbContext context, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(entities.Select(mapper.ToDto).ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new InvoiceAllocationDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InvoiceId,ContactId,AllocatedSum")] InvoiceAllocationDto dto)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Invoice), nameof(dto.Contact));
        if (!await OwnsInvoiceAndContact(userId, dto.InvoiceId, dto.ContactId)) ModelState.AddModelError(string.Empty, T["Selected invoice or contact was not found."]);
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, userId);
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsOverride = true;
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash(T["Manual split saved. Regenerate statements for the invoice's month to apply it."]);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,InvoiceId,ContactId,AllocatedSum")] InvoiceAllocationDto dto)
    {
        if (id != dto.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Invoice), nameof(dto.Contact));
        if (!await OwnsInvoiceAndContact(userId, dto.InvoiceId, dto.ContactId)) ModelState.AddModelError(string.Empty, T["Selected invoice or contact was not found."]);
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(dto);
        }
        var existing = await context.InvoiceAllocations.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        entity.CreatedAt = existing.CreatedAt;
        entity.IsOverride = true;
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash(T["Manual split updated. Regenerate statements for the invoice's month to apply it."]);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.InvoiceAllocations.Remove(entity);
            await context.SaveChangesAsync();
            Flash(T["Manual split deleted. Regenerate statements for the invoice's month to apply the change."]);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        var invoices = await context.Invoices.Include(e => e.Address).Where(e => e.UserId == userId).OrderByDescending(e => e.InvoiceDate).ToListAsync();
        ViewData["InvoiceId"] = new SelectList(invoices.Select(e => new { e.Id, Label = $"{Fmt.Date(e.InvoiceDate)} · {e.Address.Name} · {Fmt.Money(e.TotalSum)}" }), "Id", "Label");
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> OwnsInvoiceAndContact(Guid userId, Guid invoiceId, Guid contactId)
    {
        return await context.Invoices.AnyAsync(e => e.Id == invoiceId && e.UserId == userId)
               && await context.Contacts.AnyAsync(e => e.Id == contactId && e.UserId == userId);
    }
}
