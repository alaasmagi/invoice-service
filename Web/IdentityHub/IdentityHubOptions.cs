namespace Web.IdentityHub;

public sealed class IdentityHubOptions
{
    public string BaseUrl { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string? CallbackUrl { get; set; }
}
