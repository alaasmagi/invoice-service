using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class ServiceEntity : BaseEntityUserWithMetaConcurrency
{
    public string Name { get; set; } = default!;
}