using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class AllocationRuleShareDto : BaseEntityUserWithConcurrency
{
    public Guid AllocationRuleId { get; set; }

    [Display(Name = "Person")]
    public Guid ContactId { get; set; }

    public ContactDto? Contact { get; set; }

    [Display(Name = "Share")]
    public decimal? Value { get; set; }
}
