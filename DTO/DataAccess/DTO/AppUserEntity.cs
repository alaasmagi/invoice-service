using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AppUserEntity : BaseIdentityUserWithMetaConcurrency
{
    public string Fullname { get; set; } = default!;
    public string? BankIban { get; set; }
}
