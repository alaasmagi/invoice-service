using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ApiKeyRepository : BaseRepository<ApiKey, ApiKeyEntity, IMapper<ApiKey, ApiKeyEntity>>, IApiKeyRepository
{
    public ApiKeyRepository(AppDbContext repositoryDbContext, IMapper<ApiKey, ApiKeyEntity> repositoryMapper) : base(repositoryDbContext, repositoryMapper)
    {
    }

    public async Task<ApiKey?> FindByHashAsync(string keyHash)
    {
        var entity = await GetQuery().FirstOrDefaultAsync(apiKey => apiKey.KeyHash == keyHash);
        return RepositoryMapper.Map(entity);
    }
}
