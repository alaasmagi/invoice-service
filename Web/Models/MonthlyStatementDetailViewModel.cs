using DTO.DataAccess.DataAccess.DTO;

namespace Web.Models;

public class MonthlyStatementDetailViewModel
{
    public MonthlyStatementEntity Statement { get; set; } = default!;
    public IReadOnlyList<MonthlyStatementLineEntity> Lines { get; set; } = [];
    public IReadOnlyList<string> AddressNames { get; set; } = [];
    public bool CanSend { get; set; }
}
