namespace DTO.DataAccess.Web.DTO;

public class ApiKeyDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string KeyPrefix { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null;
}
