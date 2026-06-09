using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class ContactMonthlyStatementsController(AppDbContext context) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.ContactMonthlyStatements.Include(e => e.MonthlyStatement).ThenInclude(e => e.Address).Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.MonthlyStatement.Year)
            .ThenByDescending(e => e.MonthlyStatement.Month)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.ContactMonthlyStatements.Include(e => e.MonthlyStatement).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new ContactMonthlyStatementEntity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MonthlyStatementId,ContactId,Amount,EmailSent,EmailSentAt,Paid,PaidAt")] ContactMonthlyStatementEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.MonthlyStatement));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsStatementAndContact(userId, entity.MonthlyStatementId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected statement or contact was not found.");
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
        var entity = await context.ContactMonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,MonthlyStatementId,ContactId,Amount,EmailSent,EmailSentAt,Paid,PaidAt")] ContactMonthlyStatementEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.MonthlyStatement));
        ModelState.Remove(nameof(entity.Contact));
        if (!await OwnsStatementAndContact(userId, entity.MonthlyStatementId, entity.ContactId)) ModelState.AddModelError(string.Empty, "Selected statement or contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        var existing = await context.ContactMonthlyStatements.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
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
        var entity = await context.ContactMonthlyStatements.Include(e => e.MonthlyStatement).ThenInclude(e => e.Address).Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.ContactMonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.ContactMonthlyStatements.Remove(entity);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        var statements = await context.MonthlyStatements.Include(e => e.Address).Where(e => e.UserId == userId).OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).ToListAsync();
        ViewData["MonthlyStatementId"] = new SelectList(statements.Select(e => new { e.Id, Label = $"{e.Address.Name} {e.Period}" }), "Id", "Label");
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> OwnsStatementAndContact(Guid userId, Guid statementId, Guid contactId)
    {
        return await context.MonthlyStatements.AnyAsync(e => e.Id == statementId && e.UserId == userId)
               && await context.Contacts.AnyAsync(e => e.Id == contactId && e.UserId == userId);
    }
}
