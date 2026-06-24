using Application;
using Microsoft.Extensions.Configuration;
using Web.IdentityHub;

namespace Web.Configuration;

public static class RequiredConfiguration
{
    public static string AppConnectionString(IConfiguration configuration)
    {
        return Required(
            FirstValue(
                Environment.GetEnvironmentVariable("ConnectionStrings__AppConnection"),
                Environment.GetEnvironmentVariable("APP_CONNECTION_STRING"),
                configuration.GetConnectionString("AppConnection"),
                configuration["ConnectionStrings:AppConnection"]),
            "Application database connection string",
            "ConnectionStrings:AppConnection, ConnectionStrings__AppConnection, or APP_CONNECTION_STRING");
    }

    public static IdentityHubOptions IdentityHubOptions(IConfiguration configuration)
    {
        return new IdentityHubOptions
        {
            BaseUrl = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("IdentityHub__BaseUrl"),
                    Environment.GetEnvironmentVariable("IDENTITY_BASE_URL"),
                    configuration["IdentityHub:BaseUrl"]),
                "Identity-hub base URL",
                "IdentityHub:BaseUrl, IdentityHub__BaseUrl, or IDENTITY_BASE_URL"),
            ClientId = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("IdentityHub__ClientId"),
                    Environment.GetEnvironmentVariable("IDENTITY_CLIENT_ID"),
                    configuration["IdentityHub:ClientId"]),
                "Identity-hub client id",
                "IdentityHub:ClientId, IdentityHub__ClientId, or IDENTITY_CLIENT_ID"),
            ClientSecret = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("IdentityHub__ClientSecret"),
                    Environment.GetEnvironmentVariable("IDENTITY_CLIENT_SECRET"),
                    configuration["IdentityHub:ClientSecret"]),
                "Identity-hub client secret",
                "IdentityHub:ClientSecret, IdentityHub__ClientSecret, or IDENTITY_CLIENT_SECRET"),
            CallbackUrl = FirstValue(
                Environment.GetEnvironmentVariable("IdentityHub__CallbackUrl"),
                Environment.GetEnvironmentVariable("IDENTITY_CALLBACK_URL"),
                configuration["IdentityHub:CallbackUrl"])
        };
    }

    public static string EmailProvider(IConfiguration configuration)
    {
        return FirstValue(
            Environment.GetEnvironmentVariable("Email__Provider"),
            Environment.GetEnvironmentVariable("EMAIL_PROVIDER"),
            configuration["Email:Provider"]) ?? "Brevo";
    }

    public static BrevoEmailOptions BrevoEmailOptions(IConfiguration configuration)
    {
        return new BrevoEmailOptions
        {
            ApiKey = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("Brevo__ApiKey"),
                    Environment.GetEnvironmentVariable("BREVO_API_KEY"),
                    configuration["Brevo:ApiKey"]),
                "Brevo API key",
                "Brevo:ApiKey, Brevo__ApiKey, or BREVO_API_KEY"),
            SenderEmail = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("Brevo__SenderEmail"),
                    Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL"),
                    configuration["Brevo:SenderEmail"]),
                "Brevo sender email",
                "Brevo:SenderEmail, Brevo__SenderEmail, or BREVO_SENDER_EMAIL"),
            SenderName = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("Brevo__SenderName"),
                    Environment.GetEnvironmentVariable("BREVO_SENDER_NAME"),
                    configuration["Brevo:SenderName"]),
                "Brevo sender name",
                "Brevo:SenderName, Brevo__SenderName, or BREVO_SENDER_NAME")
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

    private static string? FirstValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
