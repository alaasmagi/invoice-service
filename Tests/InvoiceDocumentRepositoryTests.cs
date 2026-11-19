using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.Mapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class InvoiceDocumentRepositoryTests : IDisposable
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid OtherUser = Guid.NewGuid();
    private static readonly string Sha256 = new('a', 64);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AppDbContext _context;

    public InvoiceDocumentRepositoryTests()
    {
        _connection.Open();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private InvoiceDocumentRepository Documents() => new(_context, new InvoiceDocumentEntityMapper());

    private InvoiceDocumentRowRepository Rows() => new(_context, new InvoiceDocumentRowEntityMapper());

    private ApiKeyRepository ApiKeys() => new(_context, new ApiKeyEntityMapper());

    private async Task<InvoiceDocument> SeedDocumentAsync(Guid userId, string sha256)
    {
        var document = new InvoiceDocument
        {
            StorageKey = $"{userId}/2026/{sha256}.pdf",
            FileName = "invoice.pdf",
            Sha256 = sha256,
            UploadedAt = DateTime.UtcNow
        };
        var created = (await Documents().CreateAsync(document, userId)).Value!;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return created;
    }

    [Fact]
    public async Task FindBySha256Async_ReturnsOnlyTheUsersDocument()
    {
        var owned = await SeedDocumentAsync(Owner, Sha256);

        Assert.Equal(owned.Id, (await Documents().FindBySha256Async(Owner, Sha256))!.Id);
        Assert.Null(await Documents().FindBySha256Async(OtherUser, Sha256));
    }

    [Fact]
    public async Task SameFile_CanBeStoredByDifferentUsers_ButOnlyOncePerUser()
    {
        await SeedDocumentAsync(Owner, Sha256);
        await SeedDocumentAsync(OtherUser, Sha256);

        await Documents().CreateAsync(
            new InvoiceDocument { StorageKey = "x", FileName = "copy.pdf", Sha256 = Sha256, UploadedAt = DateTime.UtcNow },
            Owner);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task GetWithRowsAsync_ScopesByUser_AndLoadsRowsInOrder()
    {
        var document = await SeedDocumentAsync(Owner, Sha256);
        await Rows().CreateAsync(new InvoiceDocumentRow { InvoiceDocumentId = document.Id, Position = 2, AddressText = "second" });
        await Rows().CreateAsync(new InvoiceDocumentRow { InvoiceDocumentId = document.Id, Position = 1, AddressText = "first" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var forOwner = await Documents().GetWithRowsAsync(document.Id, Owner);
        var forHandler = await Documents().GetWithRowsAsync(document.Id);

        Assert.Equal(["first", "second"], forOwner!.Rows.Select(row => row.AddressText));
        Assert.Equal(2, forHandler!.Rows.Count);
        Assert.Null(await Documents().GetWithRowsAsync(document.Id, OtherUser));
    }

    [Fact]
    public async Task UpdateAsync_WorksRightAfterCreateInTheSameContext()
    {
        // The upload creates, saves and then updates the same document within one request.
        var repository = Documents();
        var created = (await repository.CreateAsync(
            new InvoiceDocument { StorageKey = "k", FileName = "a.pdf", Sha256 = Sha256, UploadedAt = DateTime.UtcNow },
            Owner)).Value!;
        await _context.SaveChangesAsync();

        created.Status = EInvoiceDocumentStatus.Processing;
        var updated = await repository.UpdateAsync(created.Id, created, actor: Owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        Assert.True(updated.Successful);
        Assert.Equal(EInvoiceDocumentStatus.Processing, (await Documents().GetWithRowsAsync(created.Id, Owner))!.Status);
    }

    [Fact]
    public async Task GetAllAsync_ListsOnlyTheUsersDocuments()
    {
        await SeedDocumentAsync(Owner, Sha256);
        await SeedDocumentAsync(OtherUser, new string('b', 64));

        var documents = (await Documents().GetAllAsync(Owner)).Value!;

        Assert.All(documents, document => Assert.Equal(Owner, document.UserId));
        Assert.Single(documents);
    }

    [Fact]
    public async Task ApiKeys_AreFoundByHashForAnyUser_AndListedPerUser()
    {
        await ApiKeys().CreateAsync(new ApiKey { Name = "gmail", KeyHash = new string('c', 64), KeyPrefix = "isk_cccc" }, Owner);
        await ApiKeys().CreateAsync(new ApiKey { Name = "other", KeyHash = new string('d', 64), KeyPrefix = "isk_dddd" }, OtherUser);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var found = await ApiKeys().FindByHashAsync(new string('c', 64));

        Assert.Equal(Owner, found!.UserId);
        Assert.Null(await ApiKeys().FindByHashAsync(new string('e', 64)));
        Assert.Single((await ApiKeys().GetAllAsync(Owner)).Value!);
    }
}
