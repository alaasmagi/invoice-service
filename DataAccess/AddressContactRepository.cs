using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AddressContactRepository : BaseRepository<AddressContact, AddressContactEntity, IMapper<AddressContact, AddressContactEntity>>, IAddressContactRepository
{
    public AddressContactRepository(DbContext repositoryDbContext, IMapper<AddressContact, AddressContactEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}