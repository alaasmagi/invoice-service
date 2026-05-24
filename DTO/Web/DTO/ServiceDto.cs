using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class ServiceDto : BaseEntityUserWithConcurrency
{
    public string Name { get; set; } = default!;
}