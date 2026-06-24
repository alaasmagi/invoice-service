using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AppUserRepository : BaseRepository<AppUser, AppUserEntity, IMapper<AppUser, AppUserEntity>>, IAppUserRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper<AppUser, AppUserEntity> _mapper;

    public AppUserRepository(AppDbContext repositoryDbContext, IMapper<AppUser, AppUserEntity> repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
        _mapper = repositoryMapper;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        var entity = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);

        return _mapper.Map(entity);
    }

    public async Task<AppUser> UpsertFromClaimsAsync(Guid id, string? fullName)
    {
        var entity = await _context.AppUsers
            .FirstOrDefaultAsync(user => user.Id == id);

        if (entity == null)
        {
            entity = new AppUserEntity
            {
                Id = id,
                Fullname = NormalizeFullName(fullName)
            };
            StampNew(entity, id);
            _context.AppUsers.Add(entity);
        }
        else if (string.IsNullOrWhiteSpace(entity.Fullname) && !string.IsNullOrWhiteSpace(fullName))
        {
            entity.Fullname = fullName.Trim();
            StampExisting(entity, id);
        }

        await _context.SaveChangesAsync();
        return _mapper.Map(entity)!;
    }

    public async Task UpdateProfileAsync(Guid id, string fullName, string? bankIban)
    {
        var entity = await _context.AppUsers
            .FirstOrDefaultAsync(user => user.Id == id);

        if (entity == null)
        {
            entity = new AppUserEntity
            {
                Id = id,
                Fullname = fullName,
                BankIban = bankIban
            };
            StampNew(entity, id);
            _context.AppUsers.Add(entity);
        }
        else
        {
            entity.Fullname = fullName;
            entity.BankIban = bankIban;
            StampExisting(entity, id);
        }

        await _context.SaveChangesAsync();
    }

    private static string NormalizeFullName(string? fullName)
    {
        return string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();
    }

    private static void StampNew(AppUserEntity entity, Guid userId)
    {
        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = userId.ToString();
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }

    private static void StampExisting(AppUserEntity entity, Guid userId)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId.ToString();
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }
}
