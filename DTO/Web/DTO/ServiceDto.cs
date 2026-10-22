using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class ServiceDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Name")]
    public string Name { get; set; } = default!;
}