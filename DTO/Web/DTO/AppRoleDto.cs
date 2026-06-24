using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class AppRoleDto : BaseEntityWithConcurrency
{
    public string Name { get; set; } = default!;
    public bool IsDefault { get; set; }
}