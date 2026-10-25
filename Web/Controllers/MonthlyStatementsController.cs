using System.Text.Json;
using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Localization;
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
            Overview = selected is { } overviewPeriod ? await periodOverviewService.GetAsync(overviewPeriod, userId) : null,
            AllocationWarnings = ReadAllocationWarnings()
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
        if (!await context.Contacts.AnyAsync(e => e.Id == dto.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(dto.ContactId), T["Selected person was not found."]);
        if (await context.MonthlyStatements.AnyAsync(e => e.UserId == userId && e.ContactId == dto.ContactId && e.Year == dto.Year && e.Month == dto.Month))
        {
            ModelState.AddModelError(string.Empty, T["This person already has a statement for that month. Edit the existing one instead."]);
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
        Flash(T["Statement created."]);
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
        if (!await context.Contacts.AnyAsync(e => e.Id == dto.ContactId && e.UserId == userId)) ModelState.AddModelError(nameof(dto.ContactId), T["Selected person was not found."]);
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
        Flash(T["Statement updated."]);
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
        if (lineCount > 0) consequences.Add(T.Plural(lineCount, "Its {0} invoice share line will be removed. The invoice itself stays.", "Its {0} invoice share lines will be removed. The invoices themselves stay."));
        if (entity.Status == EMonthlyStatementStatus.Sent) consequences.Add(T["It was already emailed to {0}. Deleting it here doesn't recall that email.", entity.Contact.FullName]);

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
        Flash(T["Statement deleted."]);
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
            Flash(T["Choose a valid month to generate statements for."], "danger");
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserId();
        var period = new BillingPeriod(year, month);
        var result = await generateMonthlyStatementService.GenerateAsync(year, month, userId);
        var statements = result.Statements;
        var overview = await periodOverviewService.GetAsync(period, userId);
        var billedInvoiceIds = await context.MonthlyStatementLines
            .Where(e => e.UserId == userId && e.MonthlyStatement.Year == year && e.MonthlyStatement.Month == month)
            .Select(e => e.InvoiceId)
            .Distinct()
            .ToListAsync();
        var skipped = result.Warnings.Select(e => e.InvoiceId).Distinct().Count(id => !billedInvoiceIds.Contains(id));
        var skippedNote = skipped > 0
            ? " " + T.Plural(skipped, "{0} invoice could not be billed. See the details below.", "{0} invoices could not be billed. See the details below.")
            : "";
        StoreAllocationWarnings(result.Warnings);

        if (statements.Count == 0)
        {
            Flash(overview.Invoices.Count > 0
                ? T["No statements were generated for {0}.", period.Label] + skippedNote
                : T["No invoices found for {0}. Add or import invoices first.", period.Label], "warning");
        }
        else
        {
            Flash(T.Plural(statements.Count, "Generated {0} statement for {1}, ready to send.", "Generated {0} statements for {1}, ready to send.", period.Label) + skippedNote, result.Warnings.Count > 0 ? "warning" : "success");
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
                    Flash(T["Statement emailed to {0}.", statement.FullName]);
                    break;
                case EMonthlyStatementStatus.Failed:
                    Flash(T["The statement for {0} couldn't be sent. Check the messaging (RabbitMQ) configuration and try again.", statement.FullName], "danger");
                    break;
                default:
                    Flash(T["Send action completed."], "info");
                    break;
            }
        }
        catch (Exception ex)
        {
            Flash(T["The statement couldn't be sent: {0}", ex.Message], "danger");
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
            Flash(T["There are no statements for {0} to send.", period.Label], "warning");
        }
        else if (result.Failed > 0)
        {
            Flash(T["{0}: sent {1}, {2} failed, {3} were already sent. Retry the failed ones from the list.", period.Label, result.Published, result.Failed, result.Skipped], "danger");
        }
        else
        {
            Flash(result.Skipped > 0
                ? T["{0}: sent {1}, {2} were already sent.", period.Label, result.Published, result.Skipped]
                : T.Plural(result.Published, "{1}: sent {0} statement.", "{1}: sent {0} statements.", period.Label));
        }

        return RedirectToAction(nameof(Index), new { period = period.Key });
    }

    private async Task PopulateSelectLists(Guid userId)
    {
        ViewData["ContactId"] = new SelectList(await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync(), "Id", "FullName");
    }

    private const string AllocationWarningsKey = "AllocationWarnings";

    private void StoreAllocationWarnings(IReadOnlyList<AllocationWarning> warnings)
    {
        if (warnings.Count > 0) TempData[AllocationWarningsKey] = JsonSerializer.Serialize(warnings);
    }

    private IReadOnlyList<AllocationWarning> ReadAllocationWarnings()
    {
        return TempData[AllocationWarningsKey] is string json
            ? JsonSerializer.Deserialize<List<AllocationWarning>>(json) ?? []
            : [];
    }
}
