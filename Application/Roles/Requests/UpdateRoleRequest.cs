namespace Application.Roles.Requests;

public record UpdateRoleRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public bool IsDefault { get; init; }
}
