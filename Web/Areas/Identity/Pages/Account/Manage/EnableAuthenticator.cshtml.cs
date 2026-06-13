using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class EnableAuthenticatorModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    UrlEncoder urlEncoder,
    ILogger<EnableAuthenticatorModel> logger) : PageModel
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private const string Issuer = "Invoice Service";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    public IReadOnlyList<string> RecoveryCodes { get; set; } = [];

    public class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await GetCurrentUserAsync();

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var is2FaTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!is2FaTokenValid)
        {
            ModelState.AddModelError("Input.Code", "Verification code is invalid.");
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", await userManager.GetUserIdAsync(user));

        if (await userManager.CountRecoveryCodesAsync(user) == 0)
        {
            RecoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
        }

        await signInManager.RefreshSignInAsync(user);
        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    private async Task<AppUser> GetCurrentUserAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new InvalidOperationException("Unable to load the authenticated user.");
        }

        return user;
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(AppUser user)
    {
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(unformattedKey!);
        var email = await userManager.GetEmailAsync(user) ?? await userManager.GetUserNameAsync(user) ?? string.Empty;
        AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey!);
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            AuthenticatorUriFormat,
            urlEncoder.Encode(Issuer),
            urlEncoder.Encode(email),
            unformattedKey);
    }

    private static string FormatKey(string unformattedKey)
    {
        return string.Join(" ", unformattedKey.Chunk(4).Select(chunk => new string(chunk))).ToLowerInvariant();
    }
}
