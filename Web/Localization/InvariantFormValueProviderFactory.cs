using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Web.Localization;

/// <summary>
/// Binds posted form values with the invariant culture. Browsers submit &lt;input type="number"&gt; and
/// &lt;input type="date"&gt; in an invariant format ("84.12", "2026-10-22") whatever the page's language,
/// so parsing them with e.g. et-EE (decimal comma) would reject valid input.
/// </summary>
public sealed class InvariantFormValueProviderFactory : IValueProviderFactory
{
    public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        var request = context.ActionContext.HttpContext.Request;
        if (!request.HasFormContentType) return;
        var form = await request.ReadFormAsync();
        context.ValueProviders.Add(new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture));
    }
}
