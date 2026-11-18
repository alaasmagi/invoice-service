namespace Application;

public static class InvoicePdfStorageKey
{
    public static string Build(Guid appUserId, int year, string sha256Hex)
    {
        if (appUserId == Guid.Empty)
        {
            throw new ArgumentException("App user id must not be empty.", nameof(appUserId));
        }

        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be between 1 and 9999.");
        }

        if (sha256Hex is not { Length: 64 } || !sha256Hex.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("SHA-256 must be 64 hexadecimal characters.", nameof(sha256Hex));
        }

        // Lower-case hex so the same content always maps to the same key.
        return $"{appUserId:D}/{year:D4}/{sha256Hex.ToLowerInvariant()}.pdf";
    }
}
