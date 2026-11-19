using Application;
using Base.Contracts.DataAccess;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using Moq;
using Xunit;

namespace Tests;

public class InvoiceDocumentReviewServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Service Telia = new() { Id = Guid.NewGuid(), UserId = Owner, Name = "Telia" };
    private static readonly Address Tehnika = new() { Id = Guid.NewGuid(), UserId = Owner, Name = "Tehnika" };

    private readonly Mock<IInvoiceDocumentRepository> _documents = new();
    private readonly Mock<IInvoiceDocumentRowRepository> _rows = new();
    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<IAddressRepository> _addresses = new();
    private readonly Mock<IParsedInvoiceImporter> _importer = new();
    private readonly Mock<IBaseUow> _uow = new();
    private readonly InvoiceDocument _document;
    private readonly InvoiceDocumentRow _first;
    private readonly InvoiceDocumentRow _second;
    private InvoiceDocument? _updatedDocument;

    public InvoiceDocumentReviewServiceTests()
    {
        _document = new InvoiceDocument { Id = Guid.NewGuid(), UserId = Owner, Status = EInvoiceDocumentStatus.NeedsReview };
        _first = UnmatchedRow(1);
        _second = UnmatchedRow(2);
        _document.Rows = [_first, _second];

        _documents.Setup(r => r.GetWithRowsAsync(_document.Id, Owner)).ReturnsAsync(_document);
        _documents
            .Setup(r => r.UpdateAsync(_document.Id, It.IsAny<InvoiceDocument>(), null, Owner))
            .ReturnsAsync((Guid _, InvoiceDocument document, string? _, Guid _) =>
            {
                _updatedDocument = document;
                return MethodResponse<InvoiceDocument>.Success(document);
            });
        _rows
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<InvoiceDocumentRow>(), null, default))
            .ReturnsAsync((Guid _, InvoiceDocumentRow row, string? _, Guid _) => MethodResponse<InvoiceDocumentRow>.Success(row));
        _services.Setup(r => r.GetAllForUserAsync(Owner)).ReturnsAsync([Telia]);
        _addresses.Setup(r => r.GetAllForUserAsync(Owner)).ReturnsAsync([Tehnika]);
        ImporterSetsOutcome(EInvoiceDocumentRowOutcome.Created);
    }

    private InvoiceDocumentRow UnmatchedRow(int position) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceDocumentId = _document.Id,
        Position = position,
        AddressText = "Mustamäe",
        Amount = 10m,
        InvoiceDate = new DateOnly(2026, 11, 5),
        Outcome = EInvoiceDocumentRowOutcome.Unmatched,
        Reason = "No matching address was found."
    };

    private void ImporterSetsOutcome(EInvoiceDocumentRowOutcome outcome)
    {
        _importer
            .Setup(i => i.CreateInvoiceAsync(It.IsAny<InvoiceDocumentRow>(), Telia.Id, Tehnika.Id, Owner))
            .Callback<InvoiceDocumentRow, Guid, Guid, Guid>((row, _, _, _) => row.Outcome = outcome)
            .Returns(Task.CompletedTask);
    }

    private InvoiceDocumentReviewService CreateSut() =>
        new(_documents.Object, _rows.Object, _services.Object, _addresses.Object, _importer.Object, _uow.Object);

    [Fact]
    public async Task Resolve_CreatesInvoiceForChosenServiceAndAddress()
    {
        var result = await CreateSut().ResolveRowAsync(_document.Id, _first.Id, Telia.Id, Tehnika.Id, Owner);

        Assert.True(result.Successful);
        Assert.Equal(EInvoiceDocumentRowOutcome.Created, _first.Outcome);
        _importer.Verify(i => i.CreateInvoiceAsync(_first, Telia.Id, Tehnika.Id, Owner));
        _rows.Verify(r => r.UpdateAsync(_first.Id, _first, null, default));
        Assert.Null(_updatedDocument);
        Assert.Equal(EInvoiceDocumentStatus.NeedsReview, result.Value!.Status);
        _uow.Verify(u => u.SaveChangesAsync());
    }

    [Fact]
    public async Task Resolve_ToDuplicate_MarksRowDuplicate()
    {
        ImporterSetsOutcome(EInvoiceDocumentRowOutcome.Duplicate);

        await CreateSut().ResolveRowAsync(_document.Id, _first.Id, Telia.Id, Tehnika.Id, Owner);

        Assert.Equal(EInvoiceDocumentRowOutcome.Duplicate, _first.Outcome);
    }

    [Fact]
    public async Task Discard_MarksRowDiscarded_WithoutCreatingInvoice()
    {
        var result = await CreateSut().DiscardRowAsync(_document.Id, _first.Id, Owner);

        Assert.True(result.Successful);
        Assert.Equal(EInvoiceDocumentRowOutcome.Discarded, _first.Outcome);
        _importer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LastUnmatchedRow_CompletesDocument()
    {
        var sut = CreateSut();

        await sut.DiscardRowAsync(_document.Id, _first.Id, Owner);
        await sut.ResolveRowAsync(_document.Id, _second.Id, Telia.Id, Tehnika.Id, Owner);

        Assert.Equal(EInvoiceDocumentStatus.Completed, _updatedDocument!.Status);
    }

    [Fact]
    public async Task Resolve_WithAnotherUsersServiceOrAddress_IsNotFound()
    {
        var foreignService = Guid.NewGuid();
        var foreignAddress = Guid.NewGuid();

        var withForeignService = await CreateSut().ResolveRowAsync(_document.Id, _first.Id, foreignService, Tehnika.Id, Owner);
        var withForeignAddress = await CreateSut().ResolveRowAsync(_document.Id, _first.Id, Telia.Id, foreignAddress, Owner);

        Assert.Equal(ErrorDefaults.Codes.NotFound, withForeignService.Error!.Code);
        Assert.Equal(ErrorDefaults.Codes.NotFound, withForeignAddress.Error!.Code);
        _importer.VerifyNoOtherCalls();
        Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, _first.Outcome);
    }

    [Fact]
    public async Task AnotherUsersDocument_IsNotFound()
    {
        var result = await CreateSut().DiscardRowAsync(_document.Id, _first.Id, Guid.NewGuid());

        Assert.Equal(ErrorDefaults.Codes.NotFound, result.Error!.Code);
        _rows.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DocumentNotInReview_IsRefused()
    {
        _document.Status = EInvoiceDocumentStatus.Completed;

        var result = await CreateSut().DiscardRowAsync(_document.Id, _first.Id, Owner);

        Assert.Equal(InvoiceDocumentReviewErrors.NotInReview, result.Error!.Code);
    }

    [Fact]
    public async Task RowAlreadyHandled_IsRefused()
    {
        _first.Outcome = EInvoiceDocumentRowOutcome.Created;

        var result = await CreateSut().ResolveRowAsync(_document.Id, _first.Id, Telia.Id, Tehnika.Id, Owner);

        Assert.Equal(InvoiceDocumentReviewErrors.RowNotUnmatched, result.Error!.Code);
        _importer.VerifyNoOtherCalls();
    }
}
