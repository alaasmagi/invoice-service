using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class AllocationRuleShareEntityMapper : IMapper<AllocationRuleShare, AllocationRuleShareEntity>
{
    public AllocationRuleShare? Map(AllocationRuleShareEntity? entity)
    {
        return entity == null ? null : new AllocationRuleShare
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AllocationRuleId = entity.AllocationRuleId,
            ContactId = entity.ContactId,
            Value = entity.Value
        };
    }

    public IEnumerable<AllocationRuleShare>? Map(IEnumerable<AllocationRuleShareEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AllocationRuleShareEntity? Map(AllocationRuleShare? entity)
    {
        return entity == null ? null : new AllocationRuleShareEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AllocationRuleId = entity.AllocationRuleId,
            ContactId = entity.ContactId,
            Value = entity.Value
        };
    }

    public IEnumerable<AllocationRuleShareEntity>? Map(IEnumerable<AllocationRuleShare>? entities)
    {
        return entities?.Select(Map)!;
    }
}
