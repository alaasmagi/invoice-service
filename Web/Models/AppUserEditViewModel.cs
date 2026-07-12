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

    [Required]
    [StringLength(16)]
    [Display(Name = "Locale")]
    public string Locale { get; set; } = "et";

    [StringLength(64)]
    [Display(Name = "Bank IBAN")]
    public string? BankIban { get; set; }
}
