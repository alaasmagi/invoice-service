using System.Security.Cryptography;
using System.Text;
using Base.DTO;
using Contracts.Application;

namespace Application;

public static class InvoicePdf
{
    public const long MaxBytes = 10 * 1024 * 1024;
    public const string ContentType = "application/pdf";

    private static readonly byte[] Header = Encoding.ASCII.GetBytes("%PDF-");

    /// <summary>Returns the validation error for a PDF of the given length and content, or null when it is acceptable.</summary>
    public static Error? Validate(long length, ReadOnlySpan<byte> content)
    {
        if (length <= 0 || content.IsEmpty)
        {
            return new Error(InvoiceDocumentErrors.EmptyFile, "The file is empty.");
        }

        if (length > MaxBytes || content.Length > MaxBytes)
        {
            return FileTooLarge();
        }

        return content.StartsWith(Header)
            ? null
            : new Error(InvoiceDocumentErrors.NotPdf, "The file is not a PDF.");
    }

    public static Error FileTooLarge()
    {
        return new Error(InvoiceDocumentErrors.FileTooLarge, $"The file is larger than {MaxBytes / 1024 / 1024} MB.");
    }

    public static string Sha256Hex(ReadOnlySpan<byte> content)
    {
        return Convert.ToHexStringLower(SHA256.HashData(content));
    }

    /// <summary>Reads at most one byte past <see cref="MaxBytes"/>, so an oversized upload is detected without buffering all of it.</summary>
    public static async Task<byte[]> ReadBoundedAsync(Stream content, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length <= MaxBytes &&
               (read = await content.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, MaxBytes + 1 - buffer.Length)), ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
