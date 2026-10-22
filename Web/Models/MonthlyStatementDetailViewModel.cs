using DTO.DataAccess.Web.DTO;

namespace Web.Models;

public class MonthlyStatementDetailViewModel
{
    public MonthlyStatementDto Statement { get; set; } = default!;
    public IReadOnlyList<MonthlyStatementLineDto> Lines { get; set; } = [];
    public IReadOnlyList<string> AddressNames { get; set; } = [];
    public bool CanSend { get; set; }
}
