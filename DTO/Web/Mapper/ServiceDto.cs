using Base.Contracts.DTO;
using Base.Domain;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ServiceDtoMapper : IMapper<ServiceDto, Service>
{
    public ServiceDto? Map(Service? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ServiceDto>? Map(IEnumerable<Service>? entities)
    {
        throw new NotImplementedException();
    }

    public Service? Map(ServiceDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Service>? Map(IEnumerable<ServiceDto>? entities)
    {
        throw new NotImplementedException();
    }
}