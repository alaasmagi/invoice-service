using Base.Domain;

namespace Application.Tests;

internal static class TestEntityStamp
{
    public static T Stamp<T>(T entity, Guid userId) where T : BaseEntityUserWithMetaConcurrency
    {
        entity.UserId = userId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CreatedBy = userId.ToString();
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
        return entity;
    }
}
