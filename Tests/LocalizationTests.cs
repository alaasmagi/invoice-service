using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web.Helpers;
using Web.Localization;
using Xunit;

namespace Tests;

public sealed class LocalizationTests
{
    private static IStringLocalizer<SharedResource> Localizer()
    {
        var options = Options.Create(new LocalizationOptions { ResourcesPath = "Resources" });
        var factory = new ResourceManagerStringLocalizerFactory(options, NullLoggerFactory.Instance);
        return new StringLocalizer<SharedResource>(factory);
    }

    private static T WithCulture<T>(string culture, string uiCulture, Func<T> action)
    {
        var (previous, previousUi) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        try { return action(); }
        finally { (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (previous, previousUi); }
    }

    private static Formatter FormatterFor(UserPreferences preferences)
    {
        var context = new DefaultHttpContext();
        context.Items[UserPreferencesCultureProvider.ItemKey] = preferences;
        return new Formatter(new HttpContextAccessor { HttpContext = context }, Localizer());
    }

    [Fact]
    public void Localizer_Estonian_ReturnsTranslation()
    {
        var text = WithCulture("et-EE", "et", () => Localizer()["Add invoice"].Value);
        Assert.Equal("Lisa arve", text);
    }

    [Fact]
    public void Localizer_English_FallsBackToKey()
    {
        var text = WithCulture("en-GB", "en", () => Localizer()["Add invoice"].Value);
        Assert.Equal("Add invoice", text);
    }

    [Fact]
    public void Plural_PicksFormByCount()
    {
        var (one, many) = WithCulture("et-EE", "et", () =>
        {
            var t = Localizer();
            return (t.Plural(1, "{0} invoice", "{0} invoices").Value, t.Plural(3, "{0} invoice", "{0} invoices").Value);
        });
        Assert.Equal("1 arve", one);
        Assert.Equal("3 arvet", many);
    }

    [Theory]
    [InlineData("et", "et")]
    [InlineData("et-EE", "et")]
    [InlineData("en_US", "en")]
    [InlineData("de", "en")]
    [InlineData(null, "en")]
    public void LanguageFor_MapsKeycloakLocale(string? locale, string expected)
    {
        Assert.Equal(expected, LocalizationSettings.LanguageFor(locale));
    }

    [Fact]
    public void Money_FollowsFormatCultureAndCurrency()
    {
        var estonian = WithCulture("et-EE", "et", () =>
            FormatterFor(new UserPreferences("et", "et-EE", "EUR", "Europe/Tallinn")).Money(1234.56m));
        var american = WithCulture("en-US", "en", () =>
            FormatterFor(new UserPreferences("en", "en-US", "USD", "America/New_York")).Money(1234.56m));

        // ICU separates with (narrow) no-break spaces, which vary by platform; compare them as plain spaces.
        Assert.Equal("1 234,56 €", System.Text.RegularExpressions.Regex.Replace(estonian, "[\u00a0\u202f]", " "));
        Assert.Equal("$1,234.56", american);
    }

    [Fact]
    public void Timestamp_ConvertsUtcToUserTimeZone()
    {
        var text = WithCulture("et-EE", "et", () =>
            FormatterFor(new UserPreferences("et", "et-EE", "EUR", "Europe/Tallinn"))
                .Timestamp(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("01.07.2026 12:00", text);
    }

    [Fact]
    public async Task FormValues_BindWithInvariantCulture_EvenForCommaDecimalCultures()
    {
        var http = new DefaultHttpContext();
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["TotalSum"] = "84.12" });
        var context = new ValueProviderFactoryContext(new ActionContext(http, new RouteData(), new ActionDescriptor()));

        var result = await WithCulture("et-EE", "et", async () =>
        {
            await new InvariantFormValueProviderFactory().CreateValueProviderAsync(context);
            return context.ValueProviders.Single().GetValue("TotalSum");
        });

        Assert.Equal(CultureInfo.InvariantCulture, result.Culture);
        Assert.Equal(84.12m, decimal.Parse(result.FirstValue!, result.Culture));
    }
}
