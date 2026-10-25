using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class AllocationRuleShareDtoMapper : IMapper<AllocationRuleShareDto, AllocationRuleShare>
{
    public AllocationRuleShareDto? Map(AllocationRuleShare? entity)
    {
        return entity == null ? null : new AllocationRuleShareDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AllocationRuleId = entity.AllocationRuleId,
            ContactId = entity.ContactId,
            Value = entity.Value
        };
    }

    public IEnumerable<AllocationRuleShareDto>? Map(IEnumerable<AllocationRuleShare>? entities)
    {
        return entities?.Select(Map)!;
    }

    public AllocationRuleShare? Map(AllocationRuleShareDto? entity)
    {
        return entity == null ? null : new AllocationRuleShare
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AllocationRuleId = entity.AllocationRuleId,
            ContactId = entity.ContactId,
            Value = entity.Value ?? 0m
        };
    }

    public IEnumerable<AllocationRuleShare>? Map(IEnumerable<AllocationRuleShareDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
