using Application;
using Infrastructure.Storage;
using Xunit;

namespace Tests;

public class StorageKeyTests
{
    private static readonly Guid AppUserId = new("8a6f0c3e-2b1d-4c5e-9f7a-1b2c3d4e5f60");
    private const string Sha256 = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    [Fact]
    public void InvoicePdfKey_FollowsUserYearHashConvention()
    {
        var key = InvoicePdfStorageKey.Build(AppUserId, 2026, Sha256);

        Assert.Equal($"8a6f0c3e-2b1d-4c5e-9f7a-1b2c3d4e5f60/2026/{Sha256}.pdf", key);
        Assert.Equal(key, StorageKey.Validate(key));
    }

    [Fact]
    public void InvoicePdfKey_LowerCasesHash_SoSameContentMapsToSameKey()
    {
        Assert.Equal(
            InvoicePdfStorageKey.Build(AppUserId, 2026, Sha256),
            InvoicePdfStorageKey.Build(AppUserId, 2026, Sha256.ToUpperInvariant()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("zz86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [InlineData("../6d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public void InvoicePdfKey_RejectsMalformedHash(string sha256)
    {
        Assert.Throws<ArgumentException>(() => InvoicePdfStorageKey.Build(AppUserId, 2026, sha256));
    }

    [Fact]
    public void InvoicePdfKey_RejectsEmptyUserAndBadYear()
    {
        Assert.Throws<ArgumentException>(() => InvoicePdfStorageKey.Build(Guid.Empty, 2026, Sha256));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvoicePdfStorageKey.Build(AppUserId, 0, Sha256));
    }

    [Theory]
    [InlineData("a.pdf")]
    [InlineData("user/2026/hash.pdf")]
    [InlineData("user/2026/arve õ.pdf")]
    public void Validate_AcceptsRelativeKeys(string key)
    {
        Assert.Equal(key, StorageKey.Validate(key));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://account.r2.cloudflarestorage.com/invoice-service/a.pdf")]
    [InlineData("s3://invoice-service/a.pdf")]
    [InlineData("/user/a.pdf")]
    [InlineData("user/")]
    [InlineData("user//a.pdf")]
    [InlineData("user/../other/a.pdf")]
    [InlineData("./a.pdf")]
    [InlineData("user\\a.pdf")]
    [InlineData("user/a\n.pdf")]
    public void Validate_RejectsNonRelativeOrMalformedKeys(string key)
    {
        Assert.Throws<ArgumentException>(() => StorageKey.Validate(key));
    }

    [Fact]
    public void Validate_RejectsKeysOverS3ByteLimit()
    {
        // 'õ' is two UTF-8 bytes, so 513 characters exceed 1024 bytes.
        Assert.Throws<ArgumentException>(() => StorageKey.Validate(new string('õ', 513)));
        Assert.Equal(1024, StorageKey.Validate(new string('a', 1024)).Length);
    }
}
