namespace Application.RoleManagement.Dtos;

public record UserRolesDto(Guid UserId, IReadOnlyList<string> Roles);
