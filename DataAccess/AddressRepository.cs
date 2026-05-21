using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AddressRepository : BaseRepository<Address, AddressEntity, IMapper<Address, AddressEntity>>, IAddressRepository
{
    public AddressRepository(DbContext repositoryDbContext, IMapper<Address, AddressEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}