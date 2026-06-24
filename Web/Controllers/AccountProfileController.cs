using Contracts.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

public class AccountProfileController(IAppUserRepository appUserRepository) : UserScopedControllerBase
{
    public async Task<IActionResult> BankDetails()
    {
        var user = await CurrentUserAsync();
        return View(new BankDetailsViewModel
        {
            Fullname = user.Fullname,
            BankIban = user.BankIban ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankDetails(BankDetailsViewModel model)
    {
        var user = await CurrentUserAsync();
        model.Fullname = model.Fullname.Trim();
        model.BankIban = NormalizeIban(model.BankIban);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await appUserRepository.UpdateProfileAsync(CurrentUserId(), model.Fullname, model.BankIban);

        TempData["StatusMessage"] = "Bank details were saved.";
        return RedirectToAction(nameof(BankDetails));
    }

    private async Task<Domain.AppUser> CurrentUserAsync()
    {
        var userId = CurrentUserId();
        var user = await appUserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            user = await appUserRepository.UpsertFromClaimsAsync(userId, User.Identity?.Name);
        }

        return user;
    }

    public static string NormalizeIban(string? value)
    {
        return string.Concat((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
    }
}
