using Base.Contracts.Message;
using Base.Keycloak.Events;
using Base.Message;
using Contracts.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class UserEventHandler(
    IServiceScopeFactory scopeFactory,
    InvoiceMessagingOptions options,
    ILogger<UserEventHandler> logger) : IBaseEventHandler<UserEventContent>
{
    public async Task HandleAsync(IBaseEventEnvelope<UserEventContent> @event, CancellationToken ct)
    {
        if (!string.Equals(@event.Type, DefaultMessageTypes.User, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Ignoring event with unexpected type {EventType}.", @event.Type);
            return;
        }

        if (!string.Equals(@event.Source, options.IdentitySource, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "Ignoring user event from source {EventSource}; expected {ExpectedSource}.",
                @event.Source,
                options.IdentitySource);
            return;
        }

        if (!Guid.TryParse(@event.Content.UserId, out var userId))
        {
            throw new InvalidOperationException($"User event contains invalid userId '{@event.Content.UserId}'.");
        }

        switch (@event.Action)
        {
            case DefaultMessageActions.UserCreated:
                using (var scope = scopeFactory.CreateScope())
                {
                    var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                    await appUserRepository.UpsertFromIdentityEventAsync(
                        userId,
                        @event.Content.Email,
                        @event.Content.Username,
                        @event.Content.FullName,
                        @event.Content.Locale,
                        ct);
                }
                logger.LogInformation("Synchronized Keycloak user {UserId}.", userId);
                break;

            case DefaultMessageActions.UserDeleted:
                using (var scope = scopeFactory.CreateScope())
                {
                    var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                    await appUserRepository.DeleteFromIdentityEventAsync(userId, ct);
                }
                logger.LogInformation("Deleted Keycloak user projection {UserId}.", userId);
                break;

            default:
                logger.LogDebug("Ignoring unsupported user event action {EventAction}.", @event.Action);
                break;
        }
    }
}
