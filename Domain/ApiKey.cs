using Base.Domain;

namespace Domain;

/// <summary>A user's API key. Only its hash is stored; the key itself is shown once when created.</summary>
public class ApiKey : BaseEntityUser
{
    public string Name { get; set; } = default!;

    public string KeyHash { get; set; } = default!;

    public string KeyPrefix { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null;
}
