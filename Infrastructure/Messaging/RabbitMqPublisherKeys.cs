namespace Infrastructure.Messaging;

/// <summary>DI keys for the per-exchange <c>IBaseEventPublisher</c> registrations.</summary>
public static class RabbitMqPublisherKeys
{
    public const string NotificationHub = "notification-hub";
    public const string AiHub = "ai-hub";
}
