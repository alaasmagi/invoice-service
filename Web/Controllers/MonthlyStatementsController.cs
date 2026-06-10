using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Controllers;

public class MonthlyStatementsController(
    AppDbContext context,
    IGenerateMonthlyStatementService generateMonthlyStatementService,
    ISendMonthlyStatementService sendMonthlyStatementService) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.MonthlyStatements.Include(e => e.Contact)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .ThenBy(e => e.Contact.FullName)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var statement = await context.MonthlyStatements.Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (statement == null) return NotFound();

        var lines = await context.MonthlyStatementLines
            .Where(e => e.UserId == userId && e.MonthlyStatementId == statement.Id)
            .OrderBy(e => e.AddressName)
            .ThenBy(e => e.InvoiceDate)
            .ThenBy(e => e.ServiceName)
            .ToListAsync();

        return View(new MonthlyStatementDetailViewModel
        {
            Statement = statement,
            Lines = lines,
            AddressNames = lines.Select(line => line.AddressName).Distinct().OrderBy(name => name).ToList(),
            CanSend = statement.Status is EMonthlyStatementStatus.Draft
                or EMonthlyStatementStatus.ReadyToSend
                or EMonthlyStatementStatus.PartiallySent
                or EMonthlyStatementStatus.Failed
                or EMonthlyStatementStatus.Sent
        });
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new MonthlyStatementEntity { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ContactId,Year,Month,TotalSum,Status,SentAt")] MonthlyStatementEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Contact));
        if (!await context.Contacts.AnyAsync(e => e.Id == entity.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(entity.ContactId), "Selected contact was not found.");
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
        var entity = await context.MonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,ContactId,Year,Month,TotalSum,Status,CreatedAt,SentAt")] MonthlyStatementEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Contact));
        if (!await context.Contacts.AnyAsync(e => e.Id == entity.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(entity.ContactId), "Selected contact was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        var existing = await context.MonthlyStatements.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
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
        var entity = await context.MonthlyStatements.Include(e => e.Contact).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.MonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.MonthlyStatements.Remove(entity);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Generate()
    {
        return View(new MonthlyStatementEntity { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([Bind("Year,Month")] MonthlyStatementEntity input)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(input);
        ModelState.Remove(nameof(input.Contact));
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var statements = await generateMonthlyStatementService.GenerateAsync(input.Year, input.Month, userId);
        if (statements.Count == 0)
        {
            var hasInvoicesInPeriod = await HasInvoicesInPeriod(input.Year, input.Month, userId);
            TempData["StatusMessage"] = hasInvoicesInPeriod
                ? "Invoices were found for the selected period, but none could be allocated. Check that the invoice addresses have active contacts on the invoice date."
                : "No invoices were found for the selected period. Generation matches invoices by Invoice Date or overlapping Period Start/End.";
            return RedirectToAction(nameof(Index));
        }

        TempData["StatusMessage"] = $"Generated {statements.Count} person monthly statement(s) for {input.Year:D4}-{input.Month:D2}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid id, string? returnTo = null)
    {
        try
        {
            await sendMonthlyStatementService.SendAsync(id, CurrentUserId());
            var userId = CurrentUserId();
            var status = await context.MonthlyStatements
                .Where(e => e.Id == id && e.UserId == userId)
                .Select(e => e.Status)
                .FirstOrDefaultAsync();

            TempData["StatusMessage"] = status switch
            {
                EMonthlyStatementStatus.Sent => "Monthly statement email was sent.",
                EMonthlyStatementStatus.Failed => "Monthly statement email could not be sent. Check email configuration and recipient address.",
                _ => "Monthly statement send action completed."
            };
        }
        catch (Exception ex)
        {
            TempData["StatusMessage"] = $"Monthly statement could not be sent: {ex.Message}";
        }

        return returnTo == nameof(Index)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task<bool> HasInvoicesInPeriod(int year, int month, Guid userId)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return await context.Invoices
            .Where(e => e.UserId == userId)
            .AnyAsync(e =>
                (e.InvoiceDate >= monthStart && e.InvoiceDate <= monthEnd)
                || (e.PeriodStart != null && e.PeriodEnd != null && e.PeriodStart.Value <= monthEnd && e.PeriodEnd.Value >= monthStart)
                || (e.PeriodStart != null && e.PeriodEnd == null && e.PeriodStart.Value >= monthStart && e.PeriodStart.Value <= monthEnd)
                || (e.PeriodStart == null && e.PeriodEnd != null && e.PeriodEnd.Value >= monthStart && e.PeriodEnd.Value <= monthEnd));
    }
}
