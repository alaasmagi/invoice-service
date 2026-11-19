using System.Text;
using Application;
using Base.Contracts.DataAccess;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Domain;
using Moq;
using Xunit;

namespace Tests;

public class InvoiceDocumentServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7 invoice body");

    private readonly Mock<IInvoiceDocumentRepository> _documents = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IInvoiceParseRequestPublisher> _publisher = new();
    private readonly Mock<IBaseUow> _uow = new();
    private readonly List<InvoiceParseCommandContent> _published = [];
    private readonly List<DateTimeOffset> _publishedExpiries = [];
    private InvoiceDocument? _saved;

    public InvoiceDocumentServiceTests()
    {
        _documents
            .Setup(r => r.CreateAsync(It.IsAny<InvoiceDocument>(), UserId))
            .ReturnsAsync((InvoiceDocument document, Guid _) =>
            {
                document.UserId = UserId;
                _saved = document;
                return MethodResponse<InvoiceDocument>.Success(document);
            });
        _documents
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<InvoiceDocument>(), null, UserId))
            .ReturnsAsync((Guid _, InvoiceDocument document, string? _, Guid _) =>
            {
                _saved = document;
                return MethodResponse<InvoiceDocument>.Success(document);
            });
        _storage
            .Setup(s => s.GetSignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://signed.example/file.pdf");
        PublisherReturns(InvoiceParseRequestResult.Ok());
    }

    private void PublisherReturns(InvoiceParseRequestResult result)
    {
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<InvoiceParseCommandContent>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceParseCommandContent, DateTimeOffset, CancellationToken>((content, expiresAt, _) =>
            {
                _published.Add(content);
                _publishedExpiries.Add(expiresAt);
            })
            .ReturnsAsync(result);
    }

    private InvoiceDocumentService CreateSut() =>
        new(_documents.Object, _storage.Object, _publisher.Object, _uow.Object, new FixedTimeProvider(Now));

    private static InvoiceDocumentUploadRequest Upload(byte[] content, long? length = null) => new()
    {
        Content = new MemoryStream(content),
        FileName = "../telia.pdf",
        Length = length ?? content.Length
    };

    private void ExistingDocument(InvoiceDocument document)
    {
        _documents.Setup(r => r.GetByIdAsync(document.Id, UserId))
            .ReturnsAsync(MethodResponse<InvoiceDocument>.Success(document));
    }

    [Theory]
    [InlineData("", InvoiceDocumentErrors.EmptyFile)]
    [InlineData("hello, not a pdf", InvoiceDocumentErrors.NotPdf)]
    public async Task Upload_RejectsInvalidFiles_WithoutStoringAnything(string content, string errorCode)
    {
        var result = await CreateSut().UploadAsync(Upload(Encoding.ASCII.GetBytes(content)), UserId);

        Assert.False(result.Successful);
        Assert.Equal(errorCode, result.Error!.Code);
        _storage.VerifyNoOtherCalls();
        _documents.Verify(r => r.CreateAsync(It.IsAny<InvoiceDocument>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Upload_RejectsOversizedFile_BeforeReadingIt()
    {
        var stream = new Mock<Stream>(MockBehavior.Strict);
        var request = new InvoiceDocumentUploadRequest
        {
            Content = stream.Object, FileName = "big.pdf", Length = InvoicePdf.MaxBytes + 1
        };

        var result = await CreateSut().UploadAsync(request, UserId);

        Assert.Equal(InvoiceDocumentErrors.FileTooLarge, result.Error!.Code);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_RejectsStreamLongerThanTheLimit_EvenWhenLengthIsUnderstated()
    {
        var oversized = new byte[InvoicePdf.MaxBytes + 10];
        Pdf.CopyTo(oversized, 0);

        var result = await CreateSut().UploadAsync(Upload(oversized, length: 100), UserId);

        Assert.Equal(InvoiceDocumentErrors.FileTooLarge, result.Error!.Code);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_StoresUnderUserYearHashKey_AndRequestsParsing()
    {
        var sha256 = InvoicePdf.Sha256Hex(Pdf);

        var result = await CreateSut().UploadAsync(Upload(Pdf), UserId);

        var expectedKey = $"{UserId}/2026/{sha256}.pdf";
        Assert.True(result.Successful);
        Assert.False(result.Value!.IsDuplicate);
        _storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), expectedKey, "application/pdf", It.IsAny<CancellationToken>()));
        _storage.Verify(s => s.GetSignedUrlAsync(expectedKey, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()));

        var document = result.Value.Document;
        Assert.Equal(expectedKey, document.StorageKey);
        Assert.Equal("telia.pdf", document.FileName);
        Assert.Equal(sha256, document.Sha256);
        Assert.Equal(Pdf.Length, document.SizeBytes);
        Assert.Equal(EInvoiceDocumentStatus.Processing, document.Status);
        Assert.Equal(Now.UtcDateTime, document.ParseRequestedAt);
        Assert.Equal(Now.AddHours(1).UtcDateTime, document.ParseExpiresAt);

        var command = Assert.Single(_published);
        Assert.Equal(document.Id.ToString(), command.DocumentId);
        Assert.Equal("https://signed.example/file.pdf", command.FileUrl);
        Assert.Equal(sha256, command.Sha256);
        Assert.Equal(Now.AddHours(1), Assert.Single(_publishedExpiries));
        _uow.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task Upload_OfSameFile_ReturnsExistingDocument_WithoutStoringOrPublishing()
    {
        var existing = new InvoiceDocument { Id = Guid.NewGuid(), UserId = UserId, Status = EInvoiceDocumentStatus.Completed };
        _documents.Setup(r => r.FindBySha256Async(UserId, InvoicePdf.Sha256Hex(Pdf))).ReturnsAsync(existing);

        var result = await CreateSut().UploadAsync(Upload(Pdf), UserId);

        Assert.True(result.Value!.IsDuplicate);
        Assert.Same(existing, result.Value.Document);
        _storage.VerifyNoOtherCalls();
        Assert.Empty(_published);
    }

    [Fact]
    public async Task Upload_WhenPublishIsNotConfirmed_KeepsDocumentUploadedWithReason()
    {
        PublisherReturns(InvoiceParseRequestResult.Failed("broker down"));

        var result = await CreateSut().UploadAsync(Upload(Pdf), UserId);

        Assert.True(result.Successful);
        Assert.Equal(EInvoiceDocumentStatus.Uploaded, result.Value!.Document.Status);
        Assert.Equal("broker down", result.Value.Document.FailureReason);
        Assert.Null(result.Value.Document.ParseRequestedAt);
        Assert.Equal(EInvoiceDocumentStatus.Uploaded, _saved!.Status);
    }

    [Fact]
    public async Task Retry_OfTimedOutDocument_SendsFreshLink()
    {
        var document = new InvoiceDocument
        {
            Id = Guid.NewGuid(), UserId = UserId, StorageKey = "key.pdf",
            Status = EInvoiceDocumentStatus.Processing, ParseExpiresAt = Now.AddMinutes(-1).UtcDateTime
        };
        ExistingDocument(document);

        var result = await CreateSut().RetryAsync(document.Id, UserId);

        Assert.True(result.Successful);
        Assert.Single(_published);
        Assert.Equal(Now.AddHours(1).UtcDateTime, result.Value!.ParseExpiresAt);
        _storage.Verify(s => s.GetSignedUrlAsync("key.pdf", TimeSpan.FromHours(1), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData(EInvoiceDocumentStatus.Completed)]
    [InlineData(EInvoiceDocumentStatus.NeedsReview)]
    [InlineData(EInvoiceDocumentStatus.Processing)]
    public async Task Retry_IsRefused_WhileParsingOrAfterParsing(EInvoiceDocumentStatus status)
    {
        var document = new InvoiceDocument
        {
            Id = Guid.NewGuid(), UserId = UserId, Status = status, ParseExpiresAt = Now.AddMinutes(30).UtcDateTime
        };
        ExistingDocument(document);

        var result = await CreateSut().RetryAsync(document.Id, UserId);

        Assert.Equal(InvoiceDocumentErrors.CannotRetry, result.Error!.Code);
        Assert.Empty(_published);
    }

    [Fact]
    public async Task Download_ForOwner_ReturnsShortLivedLink()
    {
        var document = new InvoiceDocument { Id = Guid.NewGuid(), UserId = UserId, StorageKey = "key.pdf" };
        ExistingDocument(document);

        var result = await CreateSut().GetDownloadUrlAsync(document.Id, UserId);

        Assert.Equal("https://signed.example/file.pdf", result.Value);
        _storage.Verify(s => s.GetSignedUrlAsync("key.pdf", It.Is<TimeSpan>(t => t <= TimeSpan.FromMinutes(15)), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Download_ForNonOwner_IsNotFound_AndCreatesNoLink()
    {
        var otherUser = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        _documents.Setup(r => r.GetByIdAsync(documentId, otherUser))
            .ReturnsAsync(MethodResponse<InvoiceDocument>.Failure(new Error(ErrorDefaults.Codes.NotFound, ErrorDefaults.Messages.NotFound)));

        var result = await CreateSut().GetDownloadUrlAsync(documentId, otherUser);

        Assert.Equal(ErrorDefaults.Codes.NotFound, result.Error!.Code);
        _storage.VerifyNoOtherCalls();
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
