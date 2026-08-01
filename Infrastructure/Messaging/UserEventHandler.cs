using Base.Contracts.Message;
using Base.Keycloak.Events;
using Contracts.Application;
using Contracts.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class UserEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<UserEventHandler> logger) : IBaseEventHandler<UserEventContent>
{
    public async Task HandleAsync(IBaseEventEnvelope<UserEventContent> @event, CancellationToken ct)
    {
        // Validate on arrival, before anything else. None of these improve on retry, so a failure
        // throws and the shared consumer nacks without requeue (reject → dead-letter).
        Validate(@event);
        var userId = ParseUserId(@event);

        using var scope = scopeFactory.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<IIdentityUserProjection>();

        var applied = await projection.TryProjectAsync(
            new IdentityUserProjectionInput(
                @event.Id,
                @event.Source,
                @event.Action,
                userId,
                @event.Content.Email,
                @event.Content.Username,
                @event.Content.FullName,
                @event.Content.Locale),
            ct);

        if (!applied)
        {
            // Duplicate delivery — acked silently. Log identity only, never the body.
            logger.LogDebug("Duplicate user event ignored (id={Id}, action={Action}).", @event.Id, @event.Action);
            return;
        }

        logger.LogInformation("Applied user event (id={Id}, action={Action}).", @event.Id, @event.Action);
    }

    private static void Validate(IBaseEventEnvelope<UserEventContent> @event)
    {
        // The invoice.users queue is bound to the identity publisher's events, so the AMQP routing-key
        // source is provably the identity slug. The shared consumer does not surface the raw routing
        // key, so we assert the payload source equals that constant — equivalent to comparing
        // routing-key source to payload source, closing the topic-permission bypass the contract warns
        // about.
        if (!string.Equals(@event.Source, MessagingContract.IdentitySource, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Rejecting user event {@event.Id}: source '{@event.Source}' does not match routing-key source '{MessagingContract.IdentitySource}'.");
        }

        if (string.IsNullOrWhiteSpace(@event.Tenant))
        {
            throw new InvalidOperationException($"Rejecting user event {@event.Id}: tenant is missing.");
        }

        if (!MessagingContract.IsContentVersionSupported(@event.ContentVersion))
        {
            throw new InvalidOperationException(
                $"Rejecting user event {@event.Id}: unsupported contentVersion '{@event.ContentVersion}'.");
        }
    }

    private static Guid ParseUserId(IBaseEventEnvelope<UserEventContent> @event)
    {
        if (@event.Content is null || !Guid.TryParse(@event.Content.UserId, out var userId))
        {
            throw new InvalidOperationException($"Rejecting user event {@event.Id}: invalid or missing userId.");
        }

        return userId;
    }
}
