using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Web.IdentityHub;

namespace Web.Controllers;

[Route("[controller]")]
public sealed class AuthController(
    IIdentityHubClient identityHubClient,
    IdentityHubAuthenticationService authenticationService,
    IOptions<IdentityHubOptions> identityHubOptions) : Controller
{
    private readonly IdentityHubOptions _identityHubOptions = identityHubOptions.Value;

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var loginUrl = QueryHelpers.AddQueryString(
            $"{_identityHubOptions.BaseUrl.TrimEnd('/')}/Identity/Account/Login",
            new Dictionary<string, string?>
            {
                ["clientId"] = _identityHubOptions.ClientId,
                ["redirectUri"] = BuildCallbackUrl(returnUrl)
            });

        return Redirect(loginUrl);
    }

    [HttpGet("Callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string? code, string? error, string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return View("SignInError", error);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return View("SignInError", "Missing authentication code.");
        }

        try
        {
            var claims = await identityHubClient.ExchangeCodeAsync(code);
            await authenticationService.SignInAsync(HttpContext, claims);
        }
        catch (IdentityHubException exception)
        {
            return View("SignInError", exception.Message);
        }

        return Redirect(LocalReturnUrl(returnUrl));
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("/api/auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiAuthResponse>> ApiLogin([FromBody] ApiLoginRequest request)
    {
        var loginResponse = await identityHubClient.LoginAsync(
            request.Email,
            request.Password,
            BuildCallbackUrl(request.ReturnUrl));

        if (loginResponse.RequiresTwoFactor || loginResponse.RequiresConsent)
        {
            return Ok(new ApiAuthResponse(
                false,
                loginResponse.RequiresTwoFactor,
                loginResponse.RequiresConsent,
                null,
                loginResponse.Error));
        }

        if (!string.IsNullOrWhiteSpace(loginResponse.Error))
        {
            return BadRequest(new ApiAuthResponse(false, false, false, null, loginResponse.Error));
        }

        if (string.IsNullOrWhiteSpace(loginResponse.AuthCode))
        {
            return BadRequest(new ApiAuthResponse(false, false, false, null, "MissingAuthCode"));
        }

        try
        {
            var claims = await identityHubClient.ExchangeCodeAsync(loginResponse.AuthCode);
            await authenticationService.SignInAsync(HttpContext, claims);
        }
        catch (IdentityHubException exception)
        {
            return BadRequest(new ApiAuthResponse(false, false, false, null, exception.Message));
        }

        return Ok(new ApiAuthResponse(true, false, false, LocalReturnUrl(request.ReturnUrl), null));
    }

    [HttpPost("/api/auth/external/challenge")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiAuthResponse>> ApiExternalChallenge([FromBody] ApiExternalChallengeRequest request)
    {
        var response = await identityHubClient.ExternalChallengeAsync(
            request.Provider,
            BuildCallbackUrl(request.ReturnUrl),
            request.TenantId);

        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            return BadRequest(new ApiAuthResponse(false, false, false, null, response.Error));
        }

        return Ok(new ApiAuthResponse(false, false, false, response.RedirectUrl, null));
    }

    [HttpPost("/api/auth/logout")]
    public async Task<ActionResult<ApiAuthResponse>> ApiLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new ApiAuthResponse(true, false, false, null, null));
    }

    private string BuildCallbackUrl(string? returnUrl)
    {
        var callbackUrl = _identityHubOptions.CallbackUrl;
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            callbackUrl = Url.ActionLink(nameof(Callback), "Auth")
                          ?? throw new InvalidOperationException("Could not build auth callback URL.");
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return callbackUrl;
        }

        return QueryHelpers.AddQueryString(callbackUrl, "returnUrl", LocalReturnUrl(returnUrl));
    }

    private string LocalReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home") ?? "/";
    }
}
