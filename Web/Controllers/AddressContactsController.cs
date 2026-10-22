using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

/// <summary>Residents: which contact lives at which address, and when. Drives how invoices are split.</summary>
public class AddressContactsController(AppDbContext context, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index(bool showPast = false)
    {
        var userId = CurrentUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entities = await context.AddressContacts
            .Include(e => e.Address)
            .Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Address.Name)
            .ThenBy(e => e.Contact.FullName)
            .ToListAsync();
        var pastCount = entities.Count(e => e.EndDate != null && e.EndDate < today);
        if (!showPast) entities = entities.Where(e => e.EndDate == null || e.EndDate >= today).ToList();

        ViewData["ShowPast"] = showPast;
        ViewData["PastCount"] = pastCount;
        return View(entities.Select(mapper.ToDto).ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.Include(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    public async Task<IActionResult> Create(Guid? addressId, Guid? contactId, string? returnUrl)
    {
        await PopulateForm(CurrentUserId(), returnUrl);
        return View(new AddressContactDto
        {
            AddressId = addressId ?? Guid.Empty,
            ContactId = contactId ?? Guid.Empty,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AddressId,ContactId,StartDate,EndDate")] AddressContactDto dto, string? returnUrl)
    {
        var userId = CurrentUserId();
        await Validate(dto, userId);
        if (!ModelState.IsValid)
        {
            await PopulateForm(userId, returnUrl);
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, userId);
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash(await DescribeAsync(dto, "now lives at"));
        return RedirectToLocalOr(returnUrl, nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id, string? returnUrl)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateForm(userId, returnUrl);
        return View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,AddressId,ContactId,StartDate,EndDate")] AddressContactDto dto, string? returnUrl)
    {
        if (id != dto.Id) return NotFound();
        var userId = CurrentUserId();
        await Validate(dto, userId);
        if (!ModelState.IsValid)
        {
            await PopulateForm(userId, returnUrl);
            return View(dto);
        }
        var existing = await context.AddressContacts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash("Resident updated. Regenerate statements for affected months to apply the change.");
        return RedirectToLocalOr(returnUrl, nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.AddressContacts.Include(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(new DeleteViewModel<AddressContactDto>
        {
            Item = mapper.ToDto(entity),
            Consequences =
            [
                $"Invoices for {entity.Address.Name} will no longer be split with {entity.Contact.FullName} when statements are regenerated. If they moved out, set a move-out date instead to keep the history."
            ]
        });
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
            Flash("Resident removed.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task Validate(AddressContactDto dto, Guid userId)
    {
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Address), nameof(dto.Contact));
        if (!await OwnsAddressAndContact(userId, dto.AddressId, dto.ContactId))
        {
            ModelState.AddModelError(string.Empty, "Selected address or contact was not found.");
            return;
        }
        if (dto.EndDate != null && dto.EndDate < dto.StartDate)
        {
            ModelState.AddModelError(nameof(dto.EndDate), "Move-out can't be before move-in.");
        }

        var newEnd = dto.EndDate ?? DateOnly.MaxValue;
        var overlaps = await context.AddressContacts
            .Where(e => e.UserId == userId && e.AddressId == dto.AddressId && e.ContactId == dto.ContactId && e.Id != dto.Id)
            .AnyAsync(e => e.StartDate <= newEnd && (e.EndDate == null || e.EndDate >= dto.StartDate));
        if (overlaps)
        {
            ModelState.AddModelError(string.Empty, "This person already lives at this address during these dates. Edit the existing entry instead.");
        }
    }

    private async Task<string> DescribeAsync(AddressContactDto dto, string verb)
    {
        var contact = await context.Contacts.Where(e => e.Id == dto.ContactId).Select(e => e.FullName).FirstAsync();
        var address = await context.Addresses.Where(e => e.Id == dto.AddressId).Select(e => e.Name).FirstAsync();
        return $"{contact} {verb} {address}.";
    }

    private async Task PopulateForm(Guid userId, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["AddressId"] = new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> OwnsAddressAndContact(Guid userId, Guid addressId, Guid contactId)
    {
        return await context.Addresses.AnyAsync(e => e.Id == addressId && e.UserId == userId)
               && await context.Contacts.AnyAsync(e => e.Id == contactId && e.UserId == userId);
    }
}
