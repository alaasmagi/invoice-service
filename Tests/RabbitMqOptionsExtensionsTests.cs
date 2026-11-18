using Base.Message.RabbitMQ;
using Infrastructure.Messaging;
using Xunit;

namespace Tests;

public class RabbitMqOptionsExtensionsTests
{
    [Fact]
    public void ForExchange_CopiesConnectionSettings_WithOwnExchange()
    {
        var connection = new RabbitMqOptions
        {
            Host = "rabbitmq.example.com",
            Port = 5671,
            Username = "invoice",
            Password = "secret",
            VirtualHost = "vhost",
            Exchange = "notification-hub.commands",
            PublishConfirmTimeout = TimeSpan.FromSeconds(3),
            UseTls = true,
            AcceptInvalidTlsCertificate = true
        };

        var aiHub = connection.ForExchange("ai-hub.commands");

        Assert.Equal("ai-hub.commands", aiHub.Exchange);
        Assert.Equal("notification-hub.commands", connection.Exchange);
        Assert.Equal(connection.Host, aiHub.Host);
        Assert.Equal(connection.Port, aiHub.Port);
        Assert.Equal(connection.Username, aiHub.Username);
        Assert.Equal(connection.Password, aiHub.Password);
        Assert.Equal(connection.VirtualHost, aiHub.VirtualHost);
        Assert.Equal(connection.PublishConfirmTimeout, aiHub.PublishConfirmTimeout);
        Assert.Equal(connection.UseTls, aiHub.UseTls);
        Assert.Equal(connection.AcceptInvalidTlsCertificate, aiHub.AcceptInvalidTlsCertificate);
    }
}
