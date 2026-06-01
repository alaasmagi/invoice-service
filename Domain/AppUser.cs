using Base.Domain;

namespace Domain;

public class AppUser : BaseEntity
{
    public string Fullname { get; set; } = default!;
}