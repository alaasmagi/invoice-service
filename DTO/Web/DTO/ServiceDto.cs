using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class ServiceDto : BaseEntity
{
    public string Name { get; set; } = default!;
}