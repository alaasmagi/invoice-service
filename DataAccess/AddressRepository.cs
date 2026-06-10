using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AddressRepository : BaseRepository<Address, AddressEntity, IMapper<Address, AddressEntity>>, IAddressRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper<Address, AddressEntity> _mapper;

    public AddressRepository(AppDbContext repositoryDbContext, IMapper<Address, AddressEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
        _mapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<Address>> GetAllForUserAsync(Guid userId)
    {
        var entities = await _context.Addresses
            .AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderBy(address => address.Name)
            .ToListAsync();

        return _mapper.Map(entities)?.ToList() ?? [];
    }
}
