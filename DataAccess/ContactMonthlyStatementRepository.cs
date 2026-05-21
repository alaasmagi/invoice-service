using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ContactMonthlyStatementRepository : BaseRepository<ContactMonthlyStatement, ContactMonthlyStatementEntity, IMapper<ContactMonthlyStatement, ContactMonthlyStatementEntity>>, IContactMonthlyStatementRepository
{
    public ContactMonthlyStatementRepository(DbContext repositoryDbContext, IMapper<ContactMonthlyStatement, ContactMonthlyStatementEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}