using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.AuthService;

public sealed class AuthServiceClient(
    HttpClient httpClient,
    IOptions<AuthServiceOptions> options,
    ILogger<AuthServiceClient> logger) : IAuthServiceClient
{
    private readonly AuthServiceOptions _options = options.Value;

    public async Task SyncRolesAsync(IEnumerable<RoleSyncDefinition> roles, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/client/roles/sync")
        {
            Content = JsonContent.Create(new SyncRolesRequest(roles.ToArray()))
        };
        AddClientCredentials(request);

        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);

        logger.LogDebug("Auth service role sync request completed with status {StatusCode}.", response.StatusCode);
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/client/users/{userId}/roles");
        AddClientCredentials(request);

        using var response = await httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        await EnsureSuccessAsync(response, ct);
        var body = await response.Content.ReadFromJsonAsync<UserRolesResponse>(cancellationToken: ct);
        return body?.Roles ?? [];
    }

    public async Task SetUserRolesAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/client/users/{userId}/roles")
        {
            Content = JsonContent.Create(new SetUserRolesRequest(roles.ToArray()))
        };
        AddClientCredentials(request);

        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RemoveUserRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var encodedRoleName = Uri.EscapeDataString(roleName);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/client/users/{userId}/roles/{encodedRoleName}");
        AddClientCredentials(request);

        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private void AddClientCredentials(HttpRequestMessage request)
    {
        request.Headers.Add("X-Client-Id", _options.ClientId.ToString());
        request.Headers.Add("X-Client-Secret", _options.ClientSecret);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new AuthServiceException(response.StatusCode, await TryReadErrorCodeAsync(response, ct));
    }

    private static async Task<string?> TryReadErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record SyncRolesRequest(IReadOnlyList<RoleSyncDefinition> Roles);
    private sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);
    private sealed record UserRolesResponse(Guid UserId, IReadOnlyList<string> Roles);
}
