using System.Globalization;

namespace Contracts.Application;

/// <summary>
/// Constants and helpers for the shared platform message contract (see event-contract.md).
/// Kept free of any transport dependency so both the Application and Web layers can use it.
/// </summary>
public static class MessagingContract
{
    // Outbound "invoice" command published to email-hub.commands.
    // An app publishing its own mail is both publisher and subject, so source == tenant == action.
    public const string InvoiceSource = "invoice";
    public const string InvoiceTenant = "invoice";
    public const string InvoiceAction = "invoice";
    public const string InvoiceContentVersion = "1.0";

    // Inbound identity events consumed from the invoice.users queue: the concrete publisher slug in
    // the envelope 'source' (and first routing-key segment). Matches the live wire, the shared
    // package's Base.Message.DefaultSources.IdentityHub, and the exchange name identity-hub.events.
    public const string IdentitySource = "identity-hub";

    // Consumers compare contentVersion on the MAJOR part only: a "1.0" consumer accepts "1.1".
    public const int SupportedContentMajor = 1;

    // Identity lifecycle actions (single hyphenated segment, per the contract).
    public const string UserCreated = "user-created";
    public const string UserUpdated = "user-updated";
    public const string UserEnabled = "user-enabled";
    public const string UserDisabled = "user-disabled";
    public const string UserDeleted = "user-deleted";

    /// <summary>
    /// True when the major part of <paramref name="contentVersion"/> equals the supported major.
    /// Never compares the whole string (that would dead-letter every additive "1.0" -> "1.1" change).
    /// </summary>
    public static bool IsContentVersionSupported(string? contentVersion)
    {
        if (string.IsNullOrWhiteSpace(contentVersion))
        {
            return false;
        }

        var majorSegment = contentVersion.Split('.', 2)[0];
        return int.TryParse(majorSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
               && major == SupportedContentMajor;
    }
}
