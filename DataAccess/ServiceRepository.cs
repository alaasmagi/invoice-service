using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ServiceRepository : BaseRepository<Service, ServiceEntity, IMapper<Service, ServiceEntity>>, IServiceRepository
{
    public ServiceRepository(DbContext repositoryDbContext, IMapper<Service, ServiceEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}