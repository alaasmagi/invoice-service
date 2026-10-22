using Base.Domain;

namespace Domain;

public class AppUser : BaseEntity
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Locale { get; set; } = "et";
    public string? BankIban { get; set; }

    /// <summary>UI language chosen in the app ("en", "et"); null follows the Keycloak-managed Locale.</summary>
    public string? Language { get; set; }

    /// <summary>Culture for dates and numbers (e.g. "et-EE"); null follows the language.</summary>
    public string? FormatCulture { get; set; }

    /// <summary>ISO 4217 code the user's invoices are in. Display only, amounts are never converted.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>IANA time zone used to show timestamps.</summary>
    public string TimeZone { get; set; } = "Europe/Tallinn";
}
