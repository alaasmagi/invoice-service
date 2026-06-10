using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

public class AccountProfileController(UserManager<AppUser> userManager) : UserScopedControllerBase
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

        user.Fullname = model.Fullname;
        user.BankIban = model.BankIban;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["StatusMessage"] = "Bank details were saved.";
        return RedirectToAction(nameof(BankDetails));
    }

    private async Task<AppUser> CurrentUserAsync()
    {
        var userId = CurrentUserId().ToString();
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Authenticated user was not found.");
        }

        return user;
    }

    public static string NormalizeIban(string? value)
    {
        return string.Concat((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
    }
}
