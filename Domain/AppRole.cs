using Base.Domain;

namespace Domain;

public class AppRole : BaseEntity
{
    public string Name { get; set; } = default!;
    public bool IsDefault { get; set; }
}