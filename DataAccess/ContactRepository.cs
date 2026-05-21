using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ContactRepository : BaseRepository<Contact, ContactEntity, IMapper<Contact, ContactEntity>>, IContactRepository
{
    public ContactRepository(DbContext repositoryDbContext, IMapper<Contact, ContactEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }
}