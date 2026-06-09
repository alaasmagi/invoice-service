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
        var entities = await context.MonthlyStatements.Include(e => e.Address)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var statement = await context.MonthlyStatements.Include(e => e.Address)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (statement == null) return NotFound();

        var invoices = await context.Invoices.Include(e => e.Service)
            .Where(e => e.UserId == userId && e.AddressId == statement.AddressId && e.InvoiceDate.Year == statement.Year && e.InvoiceDate.Month == statement.Month)
            .OrderBy(e => e.InvoiceDate)
            .ToListAsync();

        var contacts = await context.ContactMonthlyStatements.Include(e => e.Contact)
            .Where(e => e.UserId == userId && e.MonthlyStatementId == statement.Id)
            .OrderBy(e => e.Contact.FullName)
            .ToListAsync();

        return View(new MonthlyStatementDetailViewModel
        {
            Statement = statement,
            Invoices = invoices,
            Contacts = contacts,
            CanSend = statement.Status is EMonthlyStatementStatus.Draft or EMonthlyStatementStatus.ReadyToSend
        });
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new MonthlyStatementEntity { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AddressId,Year,Month,TotalSum,Status,SentAt")] MonthlyStatementEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        if (!await context.Addresses.AnyAsync(e => e.Id == entity.AddressId && e.UserId == userId)) ModelState.AddModelError(nameof(entity.AddressId), "Selected address was not found.");
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
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,AddressId,Year,Month,TotalSum,Status,CreatedAt,SentAt")] MonthlyStatementEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        if (!await context.Addresses.AnyAsync(e => e.Id == entity.AddressId && e.UserId == userId)) ModelState.AddModelError(nameof(entity.AddressId), "Selected address was not found.");
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
        var entity = await context.MonthlyStatements.Include(e => e.Address).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
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

    public async Task<IActionResult> Generate()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new MonthlyStatementEntity { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([Bind("AddressId,Year,Month")] MonthlyStatementEntity input)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(input);
        ModelState.Remove(nameof(input.Address));
        if (!await context.Addresses.AnyAsync(e => e.Id == input.AddressId && e.UserId == userId)) ModelState.AddModelError(nameof(input.AddressId), "Selected address was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(input);
        }

        var statement = await generateMonthlyStatementService.GenerateAsync(input.AddressId, input.Year, input.Month, userId);
        if (statement == null)
        {
            TempData["StatusMessage"] = "No invoices were found for the selected address and period.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Details), new { id = statement.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid id)
    {
        await sendMonthlyStatementService.SendAsync(id, CurrentUserId());
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["AddressId"] = new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
    }
}
