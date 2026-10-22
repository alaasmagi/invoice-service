using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

public class StatisticsController(StatisticsService statisticsService) : UserScopedControllerBase
{
    public async Task<IActionResult> Index(int months = 12)
    {
        if (!StatisticsViewModel.MonthOptions.Contains(months)) months = 12;
        return View(await statisticsService.GetAsync(months, CurrentUserId()));
    }
}
