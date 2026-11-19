using Application;
using Base.Contracts.DataAccess;
using Base.DTO;
using Base.Message;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Domain;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public class InvoiceParseResultServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly Mock<IInvoiceDocumentRepository> _documents = new();
    private readonly Mock<IInvoiceDocumentRowRepository> _rows = new();
    private readonly Mock<IParsedInvoiceImporter> _importer = new();
    private readonly Mock<IBaseUow> _uow = new();
    private readonly List<InvoiceDocumentRow> _savedRows = [];
    private readonly InvoiceDocument _document = new()
    {
        Id = Guid.NewGuid(), UserId = Owner, Status = EInvoiceDocumentStatus.Processing
    };
    private PdfInvoiceParsedDocument? _imported;
    private Guid? _importedFor;
    private InvoiceDocument? _updated;

    public InvoiceParseResultServiceTests()
    {
        _documents.Setup(r => r.GetWithRowsAsync(_document.Id, default)).ReturnsAsync(() => _document);
        _documents
            .Setup(r => r.UpdateAsync(_document.Id, It.IsAny<InvoiceDocument>(), null, default))
            .ReturnsAsync((Guid _, InvoiceDocument document, string? _, Guid _) =>
            {
                _updated = document;
                return MethodResponse<InvoiceDocument>.Success(document);
            });
        _rows
            .Setup(r => r.CreateAsync(It.IsAny<InvoiceDocumentRow>(), default))
            .ReturnsAsync((InvoiceDocumentRow row, Guid _) =>
            {
                _savedRows.Add(row);
                return MethodResponse<InvoiceDocumentRow>.Success(row);
            });
        ImporterReturns(EInvoiceDocumentRowOutcome.Created);
    }

    private void ImporterReturns(params EInvoiceDocumentRowOutcome[] outcomes)
    {
        _importer
            .Setup(i => i.ImportAsync(It.IsAny<PdfInvoiceParsedDocument>(), It.IsAny<Guid>(), _document.Id))
            .Callback<PdfInvoiceParsedDocument, Guid, Guid>((parsed, userId, _) =>
            {
                _imported = parsed;
                _importedFor = userId;
            })
            .ReturnsAsync(outcomes
                .Select((outcome, index) => new InvoiceDocumentRow
                {
                    InvoiceDocumentId = _document.Id, Position = index + 1, Outcome = outcome
                })
                .ToList());
    }

    private InvoiceParseResultService CreateSut() =>
        new(_documents.Object, _rows.Object, _importer.Object, _uow.Object, NullLogger<InvoiceParseResultService>.Instance);

    private InvoiceParsedContent Parsed(params InvoiceParsedRowContent[] rows) => new()
    {
        DocumentId = _document.Id.ToString(),
        ServiceName = "Telia",
        InvoiceDate = "2026-11-05",
        PeriodStart = "2026-10-01",
        PeriodEnd = "2026-10-31",
        Currency = "USD",
        Rows = rows.Length == 0 ? [new InvoiceParsedRowContent { AddressText = "Tehnika", Amount = "40.00" }] : [.. rows]
    };

    [Fact]
    public async Task AllRowsMatched_CompletesDocument_AndSavesRows()
    {
        await CreateSut().ApplyParsedAsync(Parsed());

        Assert.Equal(EInvoiceDocumentStatus.Completed, _updated!.Status);
        Assert.Equal("Telia", _updated.ServiceName);
        Assert.Null(_updated.FailureReason);
        Assert.Single(_savedRows);
        Assert.NotEqual(Guid.Empty, _savedRows[0].Id);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(EInvoiceDocumentRowOutcome.Unmatched)]
    [InlineData(EInvoiceDocumentRowOutcome.Created, EInvoiceDocumentRowOutcome.Unmatched)]
    public async Task AnyUnmatchedRow_NeedsReview(params EInvoiceDocumentRowOutcome[] outcomes)
    {
        ImporterReturns(outcomes);

        await CreateSut().ApplyParsedAsync(Parsed());

        Assert.Equal(EInvoiceDocumentStatus.NeedsReview, _updated!.Status);
    }

    [Fact]
    public async Task DuplicateAndInvalidRows_StillComplete()
    {
        ImporterReturns(EInvoiceDocumentRowOutcome.Duplicate, EInvoiceDocumentRowOutcome.Invalid);

        await CreateSut().ApplyParsedAsync(Parsed());

        Assert.Equal(EInvoiceDocumentStatus.Completed, _updated!.Status);
        Assert.Equal(2, _savedRows.Count);
    }

    [Fact]
    public async Task ParsedValues_AreConverted_AndOwnerComesFromDocument()
    {
        await CreateSut().ApplyParsedAsync(Parsed(
            new InvoiceParsedRowContent { AddressText = "Tehnika", Amount = "40.00" },
            new InvoiceParsedRowContent { AddressText = "Leesi", Amount = "-5.5", PeriodStart = "2026-09-15" }));

        Assert.Equal(Owner, _importedFor);
        Assert.Equal("Telia", _imported!.ServiceName);
        Assert.Equal(new DateOnly(2026, 11, 5), _imported.InvoiceDate);
        Assert.Equal(40.00m, _imported.Rows[0].Amount);
        Assert.Equal(-5.5m, _imported.Rows[1].Amount);
        Assert.Equal(new DateOnly(2026, 9, 15), _imported.Rows[1].PeriodStart);
    }

    [Fact]
    public async Task MissingAmount_ReachesImporterAsMissing()
    {
        await CreateSut().ApplyParsedAsync(Parsed(new InvoiceParsedRowContent { AddressText = "Tehnika" }));

        Assert.Null(_imported!.Rows[0].Amount);
    }

    [Theory]
    [InlineData("05.11.2026", "40.00")]
    [InlineData("2026-11-05", "40,00")]
    [InlineData("2026-11-05", "40.00 EUR")]
    public async Task UnreadableValues_FailDocument_WithoutCreatingInvoices(string invoiceDate, string amount)
    {
        var content = Parsed(new InvoiceParsedRowContent { AddressText = "Tehnika", Amount = amount });

        await CreateSut().ApplyParsedAsync(new InvoiceParsedContent
        {
            DocumentId = content.DocumentId, InvoiceDate = invoiceDate, Rows = content.Rows
        });

        Assert.Equal(EInvoiceDocumentStatus.Failed, _updated!.Status);
        Assert.NotNull(_updated.FailureReason);
        _importer.VerifyNoOtherCalls();
        Assert.Empty(_savedRows);
    }

    [Fact]
    public async Task NoRows_FailsDocument()
    {
        await CreateSut().ApplyParsedAsync(new InvoiceParsedContent { DocumentId = _document.Id.ToString() });

        Assert.Equal(EInvoiceDocumentStatus.Failed, _updated!.Status);
        _importer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FailedReply_FailsDocumentWithReason()
    {
        await CreateSut().ApplyFailedAsync(new InvoiceParseFailedContent
        {
            DocumentId = _document.Id.ToString(), Reason = "not-an-invoice", Message = "The file is a receipt."
        });

        Assert.Equal(EInvoiceDocumentStatus.Failed, _updated!.Status);
        Assert.Equal("not-an-invoice: The file is a receipt.", _updated.FailureReason);
        _importer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(EInvoiceDocumentStatus.Completed)]
    [InlineData(EInvoiceDocumentStatus.NeedsReview)]
    [InlineData(EInvoiceDocumentStatus.Failed)]
    [InlineData(EInvoiceDocumentStatus.Uploaded)]
    public async Task RepliesForDocumentsNotProcessing_AreIgnored(EInvoiceDocumentStatus status)
    {
        _document.Status = status;

        await CreateSut().ApplyParsedAsync(Parsed());
        await CreateSut().ApplyFailedAsync(new InvoiceParseFailedContent { DocumentId = _document.Id.ToString() });

        Assert.Null(_updated);
        _importer.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("8a6f0c3e-2b1d-4c5e-9f7a-1b2c3d4e5f60")]
    public async Task RepliesForUnknownDocuments_AreIgnored(string documentId)
    {
        await CreateSut().ApplyParsedAsync(new InvoiceParsedContent { DocumentId = documentId });

        Assert.Null(_updated);
        _uow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SecondReply_AfterTheFirstWasApplied_IsIgnored()
    {
        var sut = CreateSut();

        await sut.ApplyParsedAsync(Parsed());
        _document.Status = _updated!.Status;
        _updated = null;
        await sut.ApplyParsedAsync(Parsed());

        Assert.Null(_updated);
        _importer.Verify(i => i.ImportAsync(It.IsAny<PdfInvoiceParsedDocument>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handlers_RejectUnsupportedMajorVersion()
    {
        var service = new Mock<IInvoiceParseResultService>();
        var handler = new InvoiceParsedEventHandler(service.Object);
        var envelope = BaseEventEnvelope<InvoiceParsedContent>.Create(
            "ai-hub", "invoice-service", "invoice-parsed", "2.0", new InvoiceParsedContent());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(envelope));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handlers_PassContentToService()
    {
        var service = new Mock<IInvoiceParseResultService>();
        var parsed = new InvoiceParsedContent { DocumentId = "a" };
        var failed = new InvoiceParseFailedContent { DocumentId = "b" };

        await new InvoiceParsedEventHandler(service.Object).HandleAsync(
            BaseEventEnvelope<InvoiceParsedContent>.Create("ai-hub", "invoice-service", "invoice-parsed", "1.1", parsed));
        await new InvoiceParseFailedEventHandler(service.Object).HandleAsync(
            BaseEventEnvelope<InvoiceParseFailedContent>.Create("ai-hub", "invoice-service", "invoice-parse-failed", "1.0", failed));

        service.Verify(s => s.ApplyParsedAsync(parsed));
        service.Verify(s => s.ApplyFailedAsync(failed));
    }
}
