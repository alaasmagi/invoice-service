using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class AppUserEditViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Managed in Keycloak, like email and username; shown read-only.</summary>
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>The Keycloak-managed locale, used when <see cref="Language"/> is empty.</summary>
    public string Locale { get; set; } = "et";

    /// <summary>Empty means "same as my account".</summary>
    [StringLength(8)]
    [Display(Name = "Language")]
    public string? Language { get; set; }

    [StringLength(64)]
    [Display(Name = "Bank IBAN")]
    public string? BankIban { get; set; }

    /// <summary>Empty means "same as my language".</summary>
    [StringLength(16)]
    [Display(Name = "Date and number format")]
    public string? FormatCulture { get; set; }

    [Required]
    [StringLength(3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required]
    [StringLength(64)]
    [Display(Name = "Time zone")]
    public string TimeZone { get; set; } = "Europe/Tallinn";
}
