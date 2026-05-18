using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class ContactMonthlyStatementEntityMapper : IMapper<ContactMonthlyStatement, ContactMonthlyStatementEntity>
{
    public ContactMonthlyStatement? Map(ContactMonthlyStatementEntity? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactMonthlyStatement>? Map(IEnumerable<ContactMonthlyStatementEntity>? entities)
    {
        throw new NotImplementedException();
    }

    public ContactMonthlyStatementEntity? Map(ContactMonthlyStatement? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactMonthlyStatementEntity>? Map(IEnumerable<ContactMonthlyStatement>? entities)
    {
        throw new NotImplementedException();
    }
}