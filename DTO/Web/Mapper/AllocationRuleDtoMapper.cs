using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AllocationRuleDtoMapper(IMapper<AllocationRuleShareDto, AllocationRuleShare> shareMapper)
    : IMapper<AllocationRuleDto, AllocationRule>
{
    public AllocationRuleDto? Map(AllocationRule? entity)
    {
        return entity == null ? null : new AllocationRuleDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            ServiceId = entity.ServiceId,
            Method = entity.Method,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Shares = entity.Shares.Select(share => shareMapper.Map(share)!).ToList()
        };
    }

    public IEnumerable<AllocationRuleDto>? Map(IEnumerable<AllocationRule>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AllocationRule? Map(AllocationRuleDto? entity)
    {
        return entity == null ? null : new AllocationRule
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AddressId = entity.AddressId,
            ServiceId = entity.ServiceId,
            Method = entity.Method,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Shares = entity.Shares.Select(share => shareMapper.Map(share)!).ToList()
        };
    }

    public IEnumerable<AllocationRule>? Map(IEnumerable<AllocationRuleDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
