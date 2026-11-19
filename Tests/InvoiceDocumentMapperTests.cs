using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using Xunit;

namespace Tests;

public class InvoiceDocumentMapperTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void InvoiceDocument_RoundTripsAllFields()
    {
        var document = new InvoiceDocument
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            StorageKey = $"{UserId}/2026/{new string('a', 64)}.pdf",
            FileName = "telia.pdf",
            SizeBytes = 1234,
            Sha256 = new string('a', 64),
            Status = EInvoiceDocumentStatus.NeedsReview,
            FailureReason = "reason",
            UploadedAt = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc),
            ParseRequestedAt = new DateTime(2026, 11, 18, 10, 1, 0, DateTimeKind.Utc),
            ParseExpiresAt = new DateTime(2026, 11, 18, 11, 1, 0, DateTimeKind.Utc),
            ServiceName = "Telia"
        };
        var mapper = new InvoiceDocumentEntityMapper();

        var roundTripped = mapper.Map(mapper.Map(document))!;

        Assert.Equivalent(document, roundTripped);
    }

    [Fact]
    public void InvoiceDocument_MapsRowsInPositionOrder_ButNotBackToEntity()
    {
        var entity = new InvoiceDocumentEntity
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Rows =
            [
                new InvoiceDocumentRowEntity { Position = 2, AddressText = "second" },
                new InvoiceDocumentRowEntity { Position = 1, AddressText = "first" }
            ]
        };
        var mapper = new InvoiceDocumentEntityMapper();

        var document = mapper.Map(entity)!;

        Assert.Equal(["first", "second"], document.Rows.Select(row => row.AddressText));
        Assert.Empty(mapper.Map(document)!.Rows);
    }

    [Fact]
    public void InvoiceDocumentRow_RoundTripsAllFields()
    {
        var row = new InvoiceDocumentRow
        {
            Id = Guid.NewGuid(),
            InvoiceDocumentId = Guid.NewGuid(),
            Position = 3,
            AddressText = "Tallinn - Tehnika",
            Amount = 40.00m,
            InvoiceDate = new DateOnly(2026, 11, 5),
            PeriodStart = new DateOnly(2026, 10, 1),
            PeriodEnd = new DateOnly(2026, 10, 31),
            Outcome = EInvoiceDocumentRowOutcome.Created,
            Reason = "reason",
            InvoiceId = Guid.NewGuid()
        };
        var mapper = new InvoiceDocumentRowEntityMapper();

        Assert.Equivalent(row, mapper.Map(mapper.Map(row)));
    }

    [Fact]
    public void ApiKey_MapsCreatedAtFromMetadata_AndRoundTripsTheRest()
    {
        var createdAt = new DateTime(2026, 11, 18, 9, 0, 0, DateTimeKind.Utc);
        var entity = new ApiKeyEntity
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Name = "gmail",
            KeyHash = new string('b', 64),
            KeyPrefix = "isk_abcd",
            CreatedAt = createdAt,
            LastUsedAt = createdAt.AddHours(1),
            RevokedAt = createdAt.AddHours(2)
        };
        var mapper = new ApiKeyEntityMapper();

        var apiKey = mapper.Map(entity)!;
        var back = mapper.Map(apiKey)!;

        Assert.Equal(createdAt, apiKey.CreatedAt);
        Assert.False(apiKey.IsActive);
        Assert.Equal(entity.KeyHash, back.KeyHash);
        Assert.Equal(entity.KeyPrefix, back.KeyPrefix);
        Assert.Equal(entity.LastUsedAt, back.LastUsedAt);
        Assert.Equal(entity.RevokedAt, back.RevokedAt);
    }

    [Fact]
    public void Invoice_MapsDocumentLink()
    {
        var documentId = Guid.NewGuid();
        var mapper = new InvoiceEntityMapper();

        var invoice = mapper.Map(new InvoiceEntity { Id = Guid.NewGuid(), InvoiceDocumentId = documentId })!;

        Assert.Equal(documentId, invoice.InvoiceDocumentId);
        Assert.Equal(documentId, mapper.Map(invoice)!.InvoiceDocumentId);
    }

    [Theory]
    [InlineData(EInvoiceDocumentStatus.Uploaded, null, true)]
    [InlineData(EInvoiceDocumentStatus.Failed, null, true)]
    [InlineData(EInvoiceDocumentStatus.Processing, 1, false)]
    [InlineData(EInvoiceDocumentStatus.Processing, -1, true)]
    [InlineData(EInvoiceDocumentStatus.Completed, -1, false)]
    [InlineData(EInvoiceDocumentStatus.NeedsReview, -1, false)]
    public void InvoiceDocument_CanRetry_OnlyWhenNotSentOrTimedOut(
        EInvoiceDocumentStatus status, int? expiresInHours, bool expected)
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var document = new InvoiceDocument
        {
            Status = status,
            ParseExpiresAt = expiresInHours == null ? null : now.AddHours(expiresInHours.Value)
        };

        Assert.Equal(expected, document.CanRetry(now));
    }
}
