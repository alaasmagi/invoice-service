using Application;
using Microsoft.Extensions.Configuration;

namespace Web.Configuration;

public static class RequiredConfiguration
{
    public static string IdentityConnectionString(IConfiguration configuration)
    {
        return Required(
            FirstValue(
                Environment.GetEnvironmentVariable("ConnectionStrings__IdentityConnection"),
                Environment.GetEnvironmentVariable("IDENTITY_CONNECTION_STRING"),
                configuration.GetConnectionString("IdentityConnection"),
                configuration["ConnectionStrings:IdentityConnection"]),
            "Identity database connection string",
            "ConnectionStrings:IdentityConnection, ConnectionStrings__IdentityConnection, or IDENTITY_CONNECTION_STRING");
    }

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
                "Brevo:SenderName, Brevo__SenderName, or BREVO_SENDER_NAME"),
            BankAccountName = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("Bank__AccountName"),
                    Environment.GetEnvironmentVariable("BANK_ACCOUNT_NAME"),
                    configuration["Bank:AccountName"]),
                "bank account name",
                "Bank:AccountName, Bank__AccountName, or BANK_ACCOUNT_NAME"),
            BankIban = Required(
                FirstValue(
                    Environment.GetEnvironmentVariable("Bank__Iban"),
                    Environment.GetEnvironmentVariable("BANK_IBAN"),
                    configuration["Bank:Iban"]),
                "bank IBAN",
                "Bank:Iban, Bank__Iban, or BANK_IBAN")
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
