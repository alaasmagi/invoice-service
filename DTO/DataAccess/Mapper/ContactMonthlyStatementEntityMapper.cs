using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ContactMonthlyStatementEntityMapper : IMapper<ContactMonthlyStatement, ContactMonthlyStatementEntity>
{
    public ContactMonthlyStatement? Map(ContactMonthlyStatementEntity? entity)
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

    public IEnumerable<ContactMonthlyStatement>? Map(IEnumerable<ContactMonthlyStatementEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    public ContactMonthlyStatementEntity? Map(ContactMonthlyStatement? entity)
    {
        return entity == null ? null : new ContactMonthlyStatementEntity
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

    public IEnumerable<ContactMonthlyStatementEntity>? Map(IEnumerable<ContactMonthlyStatement>? entities)
    {
        return entities?.Select(Map)!;
    }
}
