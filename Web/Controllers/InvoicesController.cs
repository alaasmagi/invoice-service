using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class InvoicesController(AppDbContext context, ICreateInvoiceService createInvoiceService) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var entities = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.InvoiceDate)
            .ToListAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Invoices.Include(e => e.Address).Include(e => e.Service).Include(e => e.Allocations).ThenInclude(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new InvoiceEntity { InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ServiceId,AddressId,InvoiceDate,PeriodStart,PeriodEnd,TotalSum")] InvoiceEntity entity)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        ModelState.Remove(nameof(entity.Service));
        if (!await OwnsAddressAndService(userId, entity.AddressId, entity.ServiceId)) ModelState.AddModelError(string.Empty, "Selected address or service was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }

        await createInvoiceService.CreateAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = entity.ServiceId,
            AddressId = entity.AddressId,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            TotalSum = entity.TotalSum
        }, userId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Invoices.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,ServiceId,AddressId,InvoiceDate,PeriodStart,PeriodEnd,TotalSum")] InvoiceEntity entity)
    {
        if (id != entity.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(entity);
        ModelState.Remove(nameof(entity.Address));
        ModelState.Remove(nameof(entity.Service));
        if (!await OwnsAddressAndService(userId, entity.AddressId, entity.ServiceId)) ModelState.AddModelError(string.Empty, "Selected address or service was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(entity);
        }
        var existing = await context.Invoices.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
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
        var entity = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.Invoices.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.Invoices.Remove(entity);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["AddressId"] = new SelectList(await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
        ViewData["ServiceId"] = new SelectList(await context.Services.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
    }

    private async Task<bool> OwnsAddressAndService(Guid userId, Guid addressId, Guid serviceId)
    {
        return await context.Addresses.AnyAsync(e => e.Id == addressId && e.UserId == userId)
               && await context.Services.AnyAsync(e => e.Id == serviceId && e.UserId == userId);
    }
}
