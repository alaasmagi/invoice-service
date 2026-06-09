using Base.Contracts.DTO;
using Base.Domain;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ServiceDtoMapper : IMapper<ServiceDto, Service>
{
    public ServiceDto? Map(Service? entity)
    {
        return entity == null ? null : new ServiceDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name
        };
    }

    public IEnumerable<ServiceDto>? Map(IEnumerable<Service>? entities)
    {
        return entities?.Select(Map)!;
    }

    public Service? Map(ServiceDto? entity)
    {
        return entity == null ? null : new Service
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name
        };
    }

    public IEnumerable<Service>? Map(IEnumerable<ServiceDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
