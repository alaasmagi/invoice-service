using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Mapping;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

public class MonthlyStatementsController(
    AppDbContext context,
    IGenerateMonthlyStatementService generateMonthlyStatementService,
    ISendMonthlyStatementService sendMonthlyStatementService,
    ISendPeriodInvoiceEmailsService sendPeriodInvoiceEmailsService,
    PeriodOverviewService periodOverviewService,
    WebDtoMapper mapper) : UserScopedControllerBase
{
    private const string AllPeriods = "all";

    public async Task<IActionResult> Index(string? period, string? status)
    {
        var userId = CurrentUserId();
        var periodsWithData = (await context.MonthlyStatements.Where(e => e.UserId == userId)
                .Select(e => new { e.Year, e.Month })
                .Distinct()
                .ToListAsync())
            .Select(e => new BillingPeriod(e.Year, e.Month))
            .ToList();

        BillingPeriod? selected = period == AllPeriods
            ? null
            : BillingPeriod.TryParse(period, out var parsed)
                ? parsed
                : periodsWithData.Count > 0 ? periodsWithData.Max() : BillingPeriod.Current;

        var query = context.MonthlyStatements.Include(e => e.Contact).Where(e => e.UserId == userId);
        if (selected is { } p) query = query.Where(e => e.Year == p.Year && e.Month == p.Month);
        var hasStatus = Enum.TryParse<EMonthlyStatementStatus>(status, out var statusFilter);
        if (hasStatus) query = query.Where(e => e.Status == statusFilter);

        var statements = await query
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .ThenBy(e => e.Contact.FullName)
            .ToListAsync();

        return View(new StatementsIndexViewModel
        {
            Period = selected,
            Status = hasStatus ? statusFilter.ToString() : null,
            PeriodOptions = BillingPeriod.Options(periodsWithData),
            Statements = statements.Select(mapper.ToDto).ToList(),
            Overview = selected is { } overviewPeriod ? await periodOverviewService.GetAsync(overviewPeriod, userId) : null
        });
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
            Statement = mapper.ToDto(statement),
            Lines = lines.Select(mapper.ToDto).ToList(),
            AddressNames = lines.Select(line => line.AddressName).Distinct().OrderBy(name => name).ToList(),
            CanSend = true
        });
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists(CurrentUserId());
        return View(new MonthlyStatementDto { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ContactId,Year,Month,TotalSum,Status,SentAt")] MonthlyStatementDto dto)
    {
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Contact), nameof(dto.Lines));
        if (!await context.Contacts.AnyAsync(e => e.Id == dto.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(dto.ContactId), "Selected person was not found.");
        if (await context.MonthlyStatements.AnyAsync(e => e.UserId == userId && e.ContactId == dto.ContactId && e.Year == dto.Year && e.Month == dto.Month))
        {
            ModelState.AddModelError(string.Empty, "This person already has a statement for that month. Edit the existing one instead.");
        }
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(dto);
        }
        var entity = mapper.ToEntity(dto);
        StampNew(entity, userId);
        entity.CreatedAt = DateTime.UtcNow;
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash("Statement created.");
        return RedirectToAction(nameof(Details), new { id = entity.Id });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.MonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        await PopulateSelectLists(userId);
        return View(mapper.ToDto(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,ContactId,Year,Month,TotalSum,Status,SentAt")] MonthlyStatementDto dto)
    {
        if (id != dto.Id) return NotFound();
        var userId = CurrentUserId();
        ClearServerManagedModelState(dto);
        ClearNavigationModelState(nameof(dto.Contact), nameof(dto.Lines));
        if (!await context.Contacts.AnyAsync(e => e.Id == dto.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(dto.ContactId), "Selected person was not found.");
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(userId);
            return View(dto);
        }
        var existing = await context.MonthlyStatements.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        var entity = mapper.ToEntity(dto);
        StampExisting(entity, userId, existing);
        entity.CreatedAt = existing.CreatedAt;
        context.Update(entity);
        await context.SaveChangesAsync();
        Flash("Statement updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await context.MonthlyStatements.Include(e => e.Contact).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();

        var consequences = new List<string>();
        var lineCount = await context.MonthlyStatementLines.CountAsync(e => e.UserId == userId && e.MonthlyStatementId == entity.Id);
        if (lineCount > 0) consequences.Add($"Its {lineCount} invoice share line(s) will be removed. The invoices themselves stay.");
        if (entity.Status == EMonthlyStatementStatus.Sent) consequences.Add($"It was already emailed to {entity.Contact.FullName}. Deleting it here doesn't recall that email.");

        return View(new DeleteViewModel<MonthlyStatementDto> { Item = mapper.ToDto(entity), Consequences = consequences });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.MonthlyStatements.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return RedirectToAction(nameof(Index));

        var period = new BillingPeriod(entity.Year, entity.Month);
        context.MonthlyStatements.Remove(entity);
        await context.SaveChangesAsync();
        Flash("Statement deleted.");
        return RedirectToAction(nameof(Index), new { period = period.Key });
    }

    /// <summary>Generation now lives on the statements page; keep old links working.</summary>
    [HttpGet]
    public IActionResult Generate(string? period) => RedirectToAction(nameof(Index), new { period });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
        {
            Flash("Choose a valid month to generate statements for.", "danger");
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserId();
        var period = new BillingPeriod(year, month);
        var statements = await generateMonthlyStatementService.GenerateAsync(year, month, userId);
        var overview = await periodOverviewService.GetAsync(period, userId);
        var skipped = overview.InvoicesWithoutResidents.Count;
        var skippedNote = skipped > 0
            ? $" {skipped} invoice(s) were skipped because nobody lived at the address on the invoice date."
            : "";

        if (statements.Count == 0)
        {
            Flash(overview.Invoices.Count > 0
                ? $"No statements were generated for {period.Label}.{skippedNote}"
                : $"No invoices found for {period.Label}. Add or import invoices first.", "warning");
        }
        else
        {
            Flash($"Generated {statements.Count} statement(s) for {period.Label}, ready to send.{skippedNote}", skipped > 0 ? "warning" : "success");
        }

        return RedirectToAction(nameof(Index), new { period = period.Key });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid id, string? returnTo = null)
    {
        var userId = CurrentUserId();
        try
        {
            // Explicit single-statement action is always a deliberate human click, so allow resend:
            // clicking "Send again" on an already-sent statement publishes a fresh attempt.
            await sendMonthlyStatementService.SendAsync(id, userId, allowResend: true);
            var statement = await context.MonthlyStatements.Include(e => e.Contact)
                .Where(e => e.Id == id && e.UserId == userId)
                .Select(e => new { e.Status, e.Contact.FullName })
                .FirstOrDefaultAsync();

            switch (statement?.Status)
            {
                case EMonthlyStatementStatus.Sent:
                    Flash($"Statement emailed to {statement.FullName}.");
                    break;
                case EMonthlyStatementStatus.Failed:
                    Flash($"The statement for {statement.FullName} couldn't be sent. Check the messaging (RabbitMQ) configuration and try again.", "danger");
                    break;
                default:
                    Flash("Send action completed.", "info");
                    break;
            }
        }
        catch (Exception ex)
        {
            Flash($"The statement couldn't be sent: {ex.Message}", "danger");
        }

        if (returnTo == nameof(Index))
        {
            var period = await context.MonthlyStatements.Where(e => e.Id == id && e.UserId == userId)
                .Select(e => new { e.Year, e.Month }).FirstOrDefaultAsync();
            return RedirectToAction(nameof(Index), new { period = period == null ? null : new BillingPeriod(period.Year, period.Month).Key });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPeriod(int year, int month)
    {
        var userId = CurrentUserId();
        var period = new BillingPeriod(year, month);
        var result = await sendPeriodInvoiceEmailsService.SendPeriodAsync(year, month, userId);

        if (result.Total == 0)
        {
            Flash($"There are no statements for {period.Label} to send.", "warning");
        }
        else if (result.Failed > 0)
        {
            Flash($"{period.Label}: sent {result.Published}, {result.Failed} failed, {result.Skipped} were already sent. Retry the failed ones from the list.", "danger");
        }
        else
        {
            Flash($"{period.Label}: sent {result.Published} statement(s)" + (result.Skipped > 0 ? $", {result.Skipped} were already sent." : "."));
        }

        return RedirectToAction(nameof(Index), new { period = period.Key });
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }
}
