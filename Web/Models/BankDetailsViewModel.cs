using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public sealed class BankDetailsViewModel
{
    [Required]
    [Display(Name = "Account name")]
    [MinLength(2)]
    [MaxLength(256)]
    public string Fullname { get; set; } = default!;

    [Required]
    [Display(Name = "Bank IBAN")]
    [MinLength(5)]
    [MaxLength(34)]
    public string BankIban { get; set; } = default!;
}
