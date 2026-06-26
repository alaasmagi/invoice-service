using Base.Contracts.DTO;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public sealed class AppRoleRepository(
    AppDbContext context,
    IMapper<AppRole, AppRoleEntity> mapper) : IAppRoleRepository
{
    public async Task<IReadOnlyList<AppRole>> GetAllAsync()
    {
        var entities = await context.AppRoles
            .AsNoTracking()
            .OrderBy(role => role.IsDefault ? 0 : 1)
            .ThenBy(role => role.Name)
            .ToListAsync();

        return mapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<AppRole?> GetByIdAsync(Guid id)
    {
        var entity = await context.AppRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Id == id);

        return mapper.Map(entity);
    }

    public async Task<AppRole> CreateAsync(AppRole role)
    {
        var entity = mapper.Map(role) ?? throw new InvalidOperationException("Could not map role for creation.");
        context.AppRoles.Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map(entity) ?? throw new InvalidOperationException("Could not map created role.");
    }

    public async Task<AppRole?> UpdateAsync(AppRole role)
    {
        var entity = await context.AppRoles.FirstOrDefaultAsync(existing => existing.Id == role.Id);
        if (entity == null)
        {
            return null;
        }

        entity.Name = role.Name;
        entity.IsDefault = role.IsDefault;
        await context.SaveChangesAsync();

        return mapper.Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await context.AppRoles.FirstOrDefaultAsync(role => role.Id == id);
        if (entity == null)
        {
            return false;
        }

        context.AppRoles.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}
