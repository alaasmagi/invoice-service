using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IApiKeyRepository : IBaseRepository<ApiKey>
{
    Task<ApiKey?> FindByHashAsync(string keyHash);
}
