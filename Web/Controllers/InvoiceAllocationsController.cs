using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class InvoiceAllocationsController(AppDbContext context) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new InvoiceAllocationEntity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InvoiceId,ContactId,AllocatedSum")] InvoiceAllocationEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Invoice));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsInvoiceAndContact(userId, entity.InvoiceId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected invoice or contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        StampNew(entity, userId);
        entity.CreatedAt = DateTime.UtcNow;
        context.Add(entity);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,InvoiceId,ContactId,AllocatedSum,CreatedAt")] InvoiceAllocationEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Invoice));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsInvoiceAndContact(userId, entity.InvoiceId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected invoice or contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        var existing = await context.InvoiceAllocations.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        StampExisting(entity, userId, existing);
        entity.CreatedAt = existing.CreatedAt;
        context.Update(entity);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.InvoiceAllocations.Include(e => e.Invoice).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
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
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        var invoices = await context.Invoices.Include(e => e.Address).Where(e => e.UserId == userId).OrderByDescending(e => e.InvoiceDate).ToListAsync();
        ViewData["InvoiceId"] = new SelectList(invoices.Select(e => new { e.Id, Label = $"{e.InvoiceDate}: {e.Address.Name} ({e.TotalSum:C})" }), "Id", "Label");
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> OwnsInvoiceAndContact(Guid userId, Guid invoiceId, Guid contactId)
    {
        return await context.Invoices.AnyAsync(e => e.Id == invoiceId && e.UserId == userId)
               && await context.Contacts.AnyAsync(e => e.Id == contactId && e.UserId == userId);
    }
}
