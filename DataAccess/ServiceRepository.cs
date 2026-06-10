using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ServiceRepository : BaseRepository<Service, ServiceEntity, IMapper<Service, ServiceEntity>>, IServiceRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper<Service, ServiceEntity> _mapper;

    public ServiceRepository(AppDbContext repositoryDbContext, IMapper<Service, ServiceEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
        _mapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<Service>> GetAllForUserAsync(Guid userId)
    {
        var entities = await _context.Services
            .AsNoTracking()
            .Where(service => service.UserId == userId)
            .OrderBy(service => service.Name)
            .ToListAsync();

        return _mapper.Map(entities)?.ToList() ?? [];
    }
}
