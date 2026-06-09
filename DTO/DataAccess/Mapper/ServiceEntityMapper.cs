using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ServiceEntityMapper : IMapper<Service, ServiceEntity>
{
    public Service? Map(ServiceEntity? entity)
    {
        return entity == null ? null : new Service
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name
        };
    }

    public IEnumerable<Service>? Map(IEnumerable<ServiceEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public ServiceEntity? Map(Service? entity)
    {
        return entity == null ? null : new ServiceEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name
        };
    }

    public IEnumerable<ServiceEntity>? Map(IEnumerable<Service>? entities)
    {
        return entities?.Select(Map)!;
    }
}
