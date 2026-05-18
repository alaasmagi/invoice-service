using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class ContactMonthlyStatementDtoMapper : IMapper<ContactMonthlyStatementDto, ContactMonthlyStatement>
{
    public ContactMonthlyStatementDto? Map(ContactMonthlyStatement? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactMonthlyStatementDto>? Map(IEnumerable<ContactMonthlyStatement>? entities)
    {
        throw new NotImplementedException();
    }

    public ContactMonthlyStatement? Map(ContactMonthlyStatementDto? entity)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ContactMonthlyStatement>? Map(IEnumerable<ContactMonthlyStatementDto>? entities)
    {
        throw new NotImplementedException();
    }
}