using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class AppUserDto : BaseEntityWithConcurrency
{
    public string Fullname { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? BankIban { get; set; }
}
