using Application;
using Web.IdentityHub;

namespace Web.Configuration;

public static class RequiredConfiguration
{
    public static string AppConnectionString()
    {
        return Required(
            Environment.GetEnvironmentVariable("APP_CONNECTION_STRING"),
            "Application database connection string",
            "APP_CONNECTION_STRING");
    }

    public static IdentityHubOptions IdentityHubOptions()
    {
        return new IdentityHubOptions
        {
            BaseUrl = Required(
                Environment.GetEnvironmentVariable("IDENTITY_BASE_URL"),
                "Identity-hub base URL",
                "IDENTITY_BASE_URL"),
            ClientId = Required(
                Environment.GetEnvironmentVariable("IDENTITY_CLIENT_ID"),
                "Identity-hub client id",
                "IDENTITY_CLIENT_ID"),
            ClientSecret = Required(
                Environment.GetEnvironmentVariable("IDENTITY_CLIENT_SECRET"),
                "Identity-hub client secret",
                "IDENTITY_CLIENT_SECRET"),
            CallbackUrl = Environment.GetEnvironmentVariable("IDENTITY_CALLBACK_URL")
        };
    }

    public static string EmailProvider()
    {
        return Environment.GetEnvironmentVariable("EMAIL_PROVIDER") ?? "Brevo";
    }

    public static BrevoEmailOptions BrevoEmailOptions()
    {
        return new BrevoEmailOptions
        {
            ApiKey = Required(
                Environment.GetEnvironmentVariable("BREVO_API_KEY"),
                "Brevo API key",
                "BREVO_API_KEY"),
            SenderEmail = Required(
                Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL"),
                "Brevo sender email",
                "BREVO_SENDER_EMAIL"),
            SenderName = Required(
                Environment.GetEnvironmentVariable("BREVO_SENDER_NAME"),
                "Brevo sender name",
                "BREVO_SENDER_NAME")
        };
    }

    private static string Required(string? value, string description, string keys)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Missing required configuration: {description}. Set {keys}.");
    }

}
