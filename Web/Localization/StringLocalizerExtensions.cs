using Microsoft.Extensions.Localization;

namespace Web.Localization;

public static class StringLocalizerExtensions
{
    /// <summary>
    /// Picks the singular or plural key by count. English and Estonian share the rule
    /// (1 → singular, anything else → plural: "1 arve", "2 arvet"), so two keys are enough.
    /// </summary>
    public static LocalizedString Plural(this IStringLocalizer t, int count, string one, string many, params object[] args) =>
        t[count == 1 ? one : many, [count, .. args]];
}
