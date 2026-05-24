using Base.Domain;

namespace Domain;

public class Service : BaseEntityUser
{
    public string Name { get; set; } = default!;
}