using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class ApiKeyEntity : BaseEntityUserWithMeta
{
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [MaxLength(64)]
    public string KeyHash { get; set; } = default!;

    [MaxLength(16)]
    public string KeyPrefix { get; set; } = default!;

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
