namespace Infrastructure.AuthService;

public class AuthServiceOptions
{
    public string BaseUrl { get; set; } = default!;
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = default!;
}
