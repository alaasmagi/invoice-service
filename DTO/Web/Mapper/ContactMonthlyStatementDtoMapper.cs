using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ContactMonthlyStatementDtoMapper : IMapper<ContactMonthlyStatementDto, ContactMonthlyStatement>
{
    public ContactMonthlyStatementDto? Map(ContactMonthlyStatement? entity)
    {
        return entity == null ? null : new ContactMonthlyStatementDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            MonthlyStatementId = entity.MonthlyStatementId,
            ContactId = entity.ContactId,
            Amount = entity.Amount,
            EmailSent = entity.EmailSent,
            EmailSentAt = entity.EmailSentAt,
            Paid = entity.Paid,
            PaidAt = entity.PaidAt
        };
    }

    public IEnumerable<ContactMonthlyStatementDto>? Map(IEnumerable<ContactMonthlyStatement>? entities)
    {
        return entities?.Select(Map)!;
    }

    public ContactMonthlyStatement? Map(ContactMonthlyStatementDto? entity)
    {
        return entity == null ? null : new ContactMonthlyStatement
        {
            Id = entity.Id,
            UserId = entity.UserId,
            MonthlyStatementId = entity.MonthlyStatementId,
            ContactId = entity.ContactId,
            Amount = entity.Amount,
            EmailSent = entity.EmailSent,
            EmailSentAt = entity.EmailSentAt,
            Paid = entity.Paid,
            PaidAt = entity.PaidAt
        };
    }

    public IEnumerable<ContactMonthlyStatement>? Map(IEnumerable<ContactMonthlyStatementDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
