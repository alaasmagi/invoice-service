using Contracts.Application;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class IdentityUserProjection(
    AppDbContext context,
    IAppUserRepository users,
    IProcessedInboxMessageRepository inbox) : IIdentityUserProjection
{
    public async Task<bool> TryProjectAsync(IdentityUserProjectionInput input, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        // Insert the envelope id before applying. A duplicate delivery hits the unique index → we
        // roll back and report a duplicate so the caller acks it silently.
        var inserted = await inbox.TryInsertAsync(
            new ProcessedInboxMessage
            {
                EnvelopeId = input.EnvelopeId,
                Source = input.Source,
                Action = input.Action
            },
            ct);

        if (!inserted)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        switch (input.Action)
        {
            case MessagingContract.UserCreated:
            case MessagingContract.UserUpdated:
            case MessagingContract.UserEnabled:
            case MessagingContract.UserDisabled:
                await users.UpsertFromIdentityEventAsync(
                    input.UserId, input.Email, input.Username, input.FullName, input.Locale, ct);
                break;

            case MessagingContract.UserDeleted:
                await users.DeleteFromIdentityEventAsync(input.UserId, ct);
                break;

            // Unknown action on this queue: recorded (so we ack) but nothing to apply.
        }

        await transaction.CommitAsync(ct);
        return true;
    }
}
