using Base.Message.RabbitMQ;

namespace Infrastructure.Messaging;

public static class RabbitMqOptionsExtensions
{
    // The shared publisher reads its exchange from RabbitMqOptions, so each exchange gets a copy of the
    // connection settings with its own Exchange. Keep in sync with RabbitMqOptions when the package changes.
    public static RabbitMqOptions ForExchange(this RabbitMqOptions options, string exchange)
    {
        return new RabbitMqOptions
        {
            Host = options.Host,
            Port = options.Port,
            Username = options.Username,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            Exchange = exchange,
            PublishConfirmTimeout = options.PublishConfirmTimeout,
            UseTls = options.UseTls,
            AcceptInvalidTlsCertificate = options.AcceptInvalidTlsCertificate
        };
    }
}
