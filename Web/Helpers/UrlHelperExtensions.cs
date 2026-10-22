using Microsoft.AspNetCore.Mvc;

namespace Web.Helpers;

public static class UrlHelperExtensions
{
    /// <summary>The return URL when it is local (safe to redirect to), otherwise the fallback.</summary>
    public static string LocalOr(this IUrlHelper url, string? returnUrl, string? fallback) =>
        !string.IsNullOrEmpty(returnUrl) && url.IsLocalUrl(returnUrl) ? returnUrl : fallback ?? "/";
}
