using Base.Contracts.DTO;
using Contracts.Application;
using Domain;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

public class ApiKeysController(
    IApiKeyService apiKeyService,
    IMapper<ApiKeyDto, ApiKey> mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View(await BuildModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? name)
    {
        var result = await apiKeyService.CreateAsync(name ?? string.Empty, CurrentUserId());
        if (!result.Successful)
        {
            ModelState.AddModelError(nameof(ApiKeysViewModel.Name), T["Enter a name of up to 128 characters."]);
            return View(nameof(Index), await BuildModelAsync(name: name));
        }

        // Rendered directly instead of redirecting, so the key never lands in TempData, a URL or a cache.
        Response.Headers.CacheControl = "no-store";
        return View(nameof(Index), await BuildModelAsync(result.Value!.PlainKey));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var result = await apiKeyService.RevokeAsync(id, CurrentUserId());
        if (!result.Successful) return NotFound();
        Flash(T["API key revoked. Requests using it are rejected from now on."]);
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApiKeysViewModel> BuildModelAsync(string? newPlainKey = null, string? name = null)
    {
        var apiKeys = await apiKeyService.GetAllAsync(CurrentUserId());
        return new ApiKeysViewModel
        {
            ApiKeys = apiKeys.Select(apiKey => mapper.Map(apiKey)!).ToList(),
            NewPlainKey = newPlainKey,
            Name = name
        };
    }
}
