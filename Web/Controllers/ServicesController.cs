using DataAccess.Context;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Localization;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

public class ServicesController(AppDbContext context, WebDtoMapper mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var services = await context.Services.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync();
        ViewData["InvoiceCounts"] = await context.Invoices.Where(e => e.UserId == userId)
            .GroupBy(e => e.ServiceId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.Key, e => e.Count);
        return View(services.Select(mapper.ToDto).ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Services.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        ViewData["InvoiceCount"] = await context.Invoices.CountAsync(e => e.UserId == userId && e.ServiceId == entity.Id);
        return View(mapper.ToDto(entity));
    }

    public IActionResult Create(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new ServiceDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] ServiceDto dto, string? returnUrl)
    {
        ClearServerManagedModelState(dto);
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, CurrentUserId());
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash(T["Service \"{0}\" added.", entity.Name]);
        return RedirectToLocalOr(returnUrl, nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Services.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] ServiceDto dto)
    {
        if (id != dto.Id) return NotFound();
        ClearServerManagedModelState(dto);
        if (!ModelState.IsValid) return View(dto);
        var userId = CurrentUserId();
        var existing = await context.Services.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash(T["Service updated."]);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.Services.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(await BuildDeleteModel(entity.Id, userId, mapper.ToDto(entity)));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.Services.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            if ((await BuildDeleteModel(id, userId, mapper.ToDto(entity))).Blockers.Any())
            {
                Flash(T["This service is still used by invoices and can't be deleted."], "danger");
                return RedirectToAction(nameof(Delete), new { id });
            }
            context.Services.Remove(entity);
            await context.SaveChangesAsync();
            Flash(T["Service \"{0}\" deleted.", entity.Name]);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<DeleteViewModel<ServiceDto>> BuildDeleteModel(Guid serviceId, Guid userId, ServiceDto dto)
    {
        var invoiceCount = await context.Invoices.CountAsync(e => e.UserId == userId && e.ServiceId == serviceId);
        var lineCount = await context.MonthlyStatementLines.CountAsync(e => e.UserId == userId && e.ServiceId == serviceId);
        var ruleCount = await context.AllocationRules.CountAsync(e => e.UserId == userId && e.ServiceId == serviceId);
        var blockers = new List<string>();
        if (invoiceCount > 0) blockers.Add(T.Plural(invoiceCount, "{0} invoice uses this service. Delete or change it first.", "{0} invoices use this service. Delete or change them first."));
        else if (lineCount > 0) blockers.Add(T["Monthly statements still reference this service."]);
        if (ruleCount > 0) blockers.Add(T.Plural(ruleCount, "{0} split rule applies to this service. Delete it first.", "{0} split rules apply to this service. Delete them first."));
        return new DeleteViewModel<ServiceDto> { Item = dto, Blockers = blockers };
    }
}
