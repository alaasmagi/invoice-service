using System.Diagnostics;
using System.Security.Claims;
using DataAccess.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

public class HomeController(AppDbContext context, PeriodOverviewService periodOverviewService) : Controller
{
    public async Task<IActionResult> Index(string? period)
    {
        if (User.Identity?.IsAuthenticated != true
            || !Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return View("Landing");
        }

        var selected = BillingPeriod.TryParse(period, out var parsed) ? parsed : await DefaultPeriod(userId);
        var invoiceMonths = await context.Invoices.Where(e => e.UserId == userId)
            .Select(e => new { e.InvoiceDate.Year, e.InvoiceDate.Month })
            .Distinct()
            .ToListAsync();

        return View(new OverviewViewModel
        {
            Overview = await periodOverviewService.GetAsync(selected, userId),
            PeriodOptions = BillingPeriod.Options(invoiceMonths.Select(m => new BillingPeriod(m.Year, m.Month))),
            HasAddresses = await context.Addresses.AnyAsync(e => e.UserId == userId),
            HasServices = await context.Services.AnyAsync(e => e.UserId == userId),
            HasContacts = await context.Contacts.AnyAsync(e => e.UserId == userId),
            HasResidents = await context.AddressContacts.AnyAsync(e => e.UserId == userId)
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// The current month, unless it has no invoices yet while last month still has unsent work;
    /// then last month is the one that needs attention.
    /// </summary>
    private async Task<BillingPeriod> DefaultPeriod(Guid userId)
    {
        var current = BillingPeriod.Current;
        var previous = current.Previous;
        var currentHasInvoices = await context.Invoices.Where(e => e.UserId == userId).InPeriod(current).AnyAsync();
        if (currentHasInvoices) return current;

        var previousHasInvoices = await context.Invoices.Where(e => e.UserId == userId).InPeriod(previous).AnyAsync();
        var previousFullySent = await context.MonthlyStatements
            .Where(e => e.UserId == userId && e.Year == previous.Year && e.Month == previous.Month)
            .AllAsync(e => e.Status == Domain.EMonthlyStatementStatus.Sent);
        var previousHasStatements = await context.MonthlyStatements
            .AnyAsync(e => e.UserId == userId && e.Year == previous.Year && e.Month == previous.Month);

        return previousHasInvoices && (!previousHasStatements || !previousFullySent) ? previous : current;
    }
}
