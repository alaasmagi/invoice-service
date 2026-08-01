using Base.Keycloak.Authentication;
using Base.Message.RabbitMQ;
using Infrastructure.Messaging;

namespace Web.Configuration;

public static class RequiredConfiguration
{
    public static string AppConnectionString()
    {
        return Required(
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"),
            "Application database connection string",
            "DATABASE_CONNECTION_STRING");
    }

    public static KeycloakOptions KeycloakOptions()
    {
        var authority = Required(
            Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY"),
            "Keycloak authority",
            "KEYCLOAK_AUTHORITY");

        var clientId = Required(
            Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID"),
            "Keycloak client id",
            "KEYCLOAK_CLIENT_ID");

        return new KeycloakOptions
        {
            Authority = authority,
            ClientId = clientId,
            Audience = clientId,
            ClientSecret = Required(
                Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET"),
                "Keycloak client secret",
                "KEYCLOAK_CLIENT_SECRET"),
            RequireHttpsMetadata = !authority.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) &&
                                   !authority.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase),
            IncludeClientRoles = true
        };
    }

    public static RabbitMqOptions RabbitMqOptions()
    {
        var uri = new Uri(Required(
            Environment.GetEnvironmentVariable("RABBITMQ_URI"),
            "RabbitMQ URI",
            "RABBITMQ_URI"));

        if (!uri.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RABBITMQ_URI must use amqp:// or amqps://.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 ||
            string.IsNullOrWhiteSpace(credentials[0]) ||
            string.IsNullOrWhiteSpace(credentials[1]))
        {
            throw new InvalidOperationException("RABBITMQ_URI must include username and password.");
        }

        return new RabbitMqOptions
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? DefaultRabbitMqPort(uri.Scheme) : uri.Port,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            VirtualHost = RabbitMqVirtualHost(uri),
            Exchange = Required(
                Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE"),
                "RabbitMQ exchange",
                "RABBITMQ_EXCHANGE"),
            UseTls = uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase)
        };
    }

    public static InvoiceMessagingOptions InvoiceMessagingOptions()
    {
        return new InvoiceMessagingOptions
        {
            // Pre-declared on the broker, bound to identity-hub.events with identity-hub.invoice.#.
            Queue = Required(
                Environment.GetEnvironmentVariable("RABBITMQ_QUEUE"),
                "RabbitMQ consumer queue",
                "RABBITMQ_QUEUE")
        };
    }

    public static GlitchtipOptions GlitchtipOptions()
    {
        return new GlitchtipOptions(
            Environment.GetEnvironmentVariable("GLITCHTIP_DSN"),
            Environment.GetEnvironmentVariable("GLITCHTIP_RELEASE") ?? "invoice-service");
    }

    private static int DefaultRabbitMqPort(string scheme)
    {
        return scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase) ? 5671 : 5672;
    }

    private static string RabbitMqVirtualHost(Uri uri)
    {
        var path = uri.AbsolutePath.Trim('/');
        return string.IsNullOrWhiteSpace(path)
            ? "/"
            : Uri.UnescapeDataString(path);
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
