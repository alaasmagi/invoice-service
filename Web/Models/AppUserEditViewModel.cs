using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public sealed class AppUserEditViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Managed in Keycloak; shown read-only.</summary>
    [Display(Name = "Language")]
    public string Locale { get; set; } = "et";

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
