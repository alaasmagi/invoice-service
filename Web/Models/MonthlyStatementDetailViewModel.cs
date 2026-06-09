using DTO.DataAccess.DataAccess.DTO;

namespace Web.Models;

public class MonthlyStatementDetailViewModel
{
    public MonthlyStatementEntity Statement { get; set; } = default!;
    public IReadOnlyList<InvoiceEntity> Invoices { get; set; } = [];
    public IReadOnlyList<ContactMonthlyStatementEntity> Contacts { get; set; } = [];
    public bool CanSend { get; set; }
}
