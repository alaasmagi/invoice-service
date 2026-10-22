using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

public class ContactsController(AppDbContext context, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var contacts = await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync();
        return View(contacts.Select(mapper.ToDto).ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Contacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();

        var residencies = await context.AddressContacts.Include(e => e.Address)
            .Where(e => e.UserId == userId && e.ContactId == entity.Id)
            .OrderBy(e => e.EndDate != null).ThenByDescending(e => e.StartDate)
            .ToListAsync();
        var statements = await context.MonthlyStatements
            .Where(e => e.UserId == userId && e.ContactId == entity.Id)
            .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month)
            .Take(12)
            .ToListAsync();

        return View(new ContactDetailsViewModel
        {
            Contact = mapper.ToDto(entity),
            Residencies = residencies.Select(mapper.ToDto).ToList(),
            Statements = statements.Select(mapper.ToDto).ToList()
        });
    }

    public IActionResult Create(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new ContactDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Email,Phone")] ContactDto dto, string? returnUrl, string? next)
    {
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.AddressContacts), nameof(dto.InvoiceAllocations), nameof(dto.MonthlyStatements));
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, CurrentUserId());
        context.Add(entity);
        await context.SaveChangesAsync();

        if (next == "another")
        {
            Flash($"{entity.FullName} added. Add the next contact.");
            return RedirectToAction(nameof(Create), new { returnUrl });
        }
        Flash($"{entity.FullName} added.");
        return RedirectToLocalOr(returnUrl, nameof(Details), new { id = entity.Id });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Contacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,FullName,Email,Phone")] ContactDto dto)
    {
        if (id != dto.Id) return NotFound();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.AddressContacts), nameof(dto.InvoiceAllocations), nameof(dto.MonthlyStatements));
        if (!ModelState.IsValid) return View(dto);
        var userId = CurrentUserId();
        var existing = await context.Contacts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash("Contact updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Contacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(await BuildDeleteModel(entity.Id, userId, mapper.ToDto(entity)));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.Contacts.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            var model = await BuildDeleteModel(id, userId, mapper.ToDto(entity));
            if (model.Blockers.Any())
            {
                Flash("This contact still has statements or allocations and can't be deleted.", "danger");
                return RedirectToAction(nameof(Delete), new { id });
            }
            context.Contacts.Remove(entity);
            await context.SaveChangesAsync();
            Flash($"{entity.FullName} deleted.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<DeleteViewModel<ContactDto>> BuildDeleteModel(Guid contactId, Guid userId, ContactDto dto)
    {
        var statementCount = await context.MonthlyStatements.CountAsync(e => e.UserId == userId && e.ContactId == contactId);
        var allocationCount = await context.InvoiceAllocations.CountAsync(e => e.UserId == userId && e.ContactId == contactId);
        var residencyCount = await context.AddressContacts.CountAsync(e => e.UserId == userId && e.ContactId == contactId);

        var blockers = new List<string>();
        if (statementCount > 0) blockers.Add($"{dto.FullName} has {statementCount} monthly statement(s). Delete them first, or keep the contact for history.");
        if (allocationCount > 0) blockers.Add($"{dto.FullName} has {allocationCount} invoice allocation(s). Delete them first.");

        return new DeleteViewModel<ContactDto>
        {
            Item = dto,
            Blockers = blockers,
            Consequences = residencyCount > 0 ? [$"{residencyCount} resident record(s) at addresses will also be deleted."] : []
        };
    }
}
