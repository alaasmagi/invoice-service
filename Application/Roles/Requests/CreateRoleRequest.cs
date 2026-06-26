namespace Application.Roles.Requests;

public record CreateRoleRequest
{
    public string Name { get; init; } = default!;
    public bool IsDefault { get; init; }
}
