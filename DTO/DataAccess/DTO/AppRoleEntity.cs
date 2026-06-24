using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AppRoleEntity : BaseEntityWithMetaConcurrency
{
    public string Name { get; set; } = default!;
    public bool IsDefault { get; set; }
}