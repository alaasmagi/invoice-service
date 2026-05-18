using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ServiceEntityMapper : IMapper<Service, ServiceEntity>
{
    public Service? Map(ServiceEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Service>? Map(IEnumerable<ServiceEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public ServiceEntity? Map(Service? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ServiceEntity>? Map(IEnumerable<Service>? entities)
    {
        throw new NotImplementedException();
    }
}