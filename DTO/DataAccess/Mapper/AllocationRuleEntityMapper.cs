using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AllocationRuleEntityMapper(IMapper<AllocationRuleShare, AllocationRuleShareEntity> shareMapper)
    : IMapper<AllocationRule, AllocationRuleEntity>
{
    public AllocationRule? Map(AllocationRuleEntity? entity)
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

    public IEnumerable<AllocationRule>? Map(IEnumerable<AllocationRuleEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AllocationRuleEntity? Map(AllocationRule? entity)
    {
        return entity == null ? null : new AllocationRuleEntity
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

    public IEnumerable<AllocationRuleEntity>? Map(IEnumerable<AllocationRule>? entities)
    {
        return entities?.Select(Map)!;
    }
}
