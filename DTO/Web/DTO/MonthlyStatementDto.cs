using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class MonthlyStatementDto : BaseEntityUserWithConcurrency
{
    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<InvoiceDto> Invoices { get; set; }
        = new List<InvoiceDto>();

    public ICollection<ContactMonthlyStatementDto> Contacts { get; set; }
        = new List<ContactMonthlyStatementDto>();

    public string Period => $"{Year:D4}-{Month:D2}";
}