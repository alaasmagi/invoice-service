using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class AddressContactsController(AppDbContext context) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.AddressContacts
            .Include(e => e.Address)
            .Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Address.Name)
            .ThenBy(e => e.Contact.FullName)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.Include(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new AddressContactEntity { StartDate = DateOnly.FromDateTime(DateTime.UtcNow) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AddressId,ContactId,StartDate,EndDate")] AddressContactEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsAddressAndContact(userId, entity.AddressId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected address or contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        StampNew(entity, userId);
        context.Add(entity);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,AddressId,ContactId,StartDate,EndDate")] AddressContactEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsAddressAndContact(userId, entity.AddressId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected address or contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        var existing = await context.AddressContacts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.Include(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.AddressContacts.Remove(entity);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["AddressId"] = new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> OwnsAddressAndContact(Guid userId, Guid addressId, Guid contactId)
    {
        return await context.Addresses.AnyAsync(e => e.Id == addressId && e.UserId == userId)
               && await context.Contacts.AnyAsync(e => e.Id == contactId && e.UserId == userId);
    }
}
