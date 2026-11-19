using System.Text.RegularExpressions;
using System.Xml.Linq;
using Domain;
using DTO.DataAccess.Web.DTO;
using DTO.DataAccess.Web.Mapper;
using Xunit;

namespace Tests;

public class InvoiceDocumentWebTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static InvoiceDocumentDetailsDtoMapper Mapper() => new(new FixedTimeProvider(Now));

    [Fact]
    public void DetailsDto_FlagsTimedOutDocument_AsRetryable()
    {
        var dto = Mapper().Map(new InvoiceDocument
        {
            Id = Guid.NewGuid(),
            FileName = "telia.pdf",
            StorageKey = "secret/key.pdf",
            Status = EInvoiceDocumentStatus.Processing,
            ParseExpiresAt = Now.AddMinutes(-1).UtcDateTime
        })!;

        Assert.True(dto.IsTimedOut);
        Assert.True(dto.CanRetry);
    }

    [Fact]
    public void DetailsDto_ForDocumentStillProcessing_IsNotRetryable()
    {
        var dto = Mapper().Map(new InvoiceDocument
        {
            Status = EInvoiceDocumentStatus.Processing,
            ParseExpiresAt = Now.AddMinutes(30).UtcDateTime
        })!;

        Assert.False(dto.IsTimedOut);
        Assert.False(dto.CanRetry);
    }

    [Fact]
    public void DetailsDto_CarriesRowsWithOutcomes_AndNoStorageKey()
    {
        var invoiceId = Guid.NewGuid();
        var dto = Mapper().Map(new InvoiceDocument
        {
            StorageKey = "secret/key.pdf",
            Rows =
            [
                new InvoiceDocumentRow { Position = 1, AddressText = "Tehnika", Amount = 40m, Outcome = EInvoiceDocumentRowOutcome.Created, InvoiceId = invoiceId },
                new InvoiceDocumentRow { Position = 2, AddressText = "Mustamäe", Outcome = EInvoiceDocumentRowOutcome.Unmatched, Reason = "No match" }
            ]
        })!;

        Assert.Equal(invoiceId, dto.Rows[0].InvoiceId);
        Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, dto.Rows[1].Outcome);
        Assert.Equal("No match", dto.Rows[1].Reason);
        Assert.Null(typeof(InvoiceDocumentDetailsDto).GetProperty("StorageKey"));
    }

    [Fact]
    public void ApiKeyDto_NeverCarriesTheHash()
    {
        var dto = new ApiKeyDtoMapper().Map(new ApiKey
        {
            Id = Guid.NewGuid(), Name = "gmail", KeyPrefix = "isk_abcdefgh", KeyHash = new string('a', 64)
        })!;

        Assert.Equal("isk_abcdefgh", dto.KeyPrefix);
        Assert.True(dto.IsActive);
        Assert.Null(typeof(ApiKeyDto).GetProperty("KeyHash"));
    }

    [Fact]
    public void InvoiceDto_KeepsTheDocumentLink()
    {
        var documentId = Guid.NewGuid();
        var mapper = new InvoiceDtoMapper();

        var dto = mapper.Map(new Invoice { Id = Guid.NewGuid(), InvoiceDocumentId = documentId })!;

        Assert.Equal(documentId, dto.InvoiceDocumentId);
        Assert.Equal(documentId, mapper.Map(dto)!.InvoiceDocumentId);
    }

    [Theory]
    [InlineData("Views/InvoiceDocuments/Index.cshtml")]
    [InlineData("Views/InvoiceDocuments/Details.cshtml")]
    [InlineData("Views/ApiKeys/Index.cshtml")]
    [InlineData("Views/InvoiceImports/Upload.cshtml")]
    [InlineData("Controllers/InvoiceDocumentsController.cs")]
    [InlineData("Controllers/ApiKeysController.cs")]
    [InlineData("Controllers/InvoiceImportsController.cs")]
    public void NewTexts_HaveEstonianTranslations(string relativePath)
    {
        var webRoot = FindWebProject();
        var source = File.ReadAllText(Path.Combine(webRoot, relativePath));
        var translated = XDocument.Load(Path.Combine(webRoot, "Resources/Localization/SharedResource.et.resx"))
            .Root!.Elements("data").Select(data => (string)data.Attribute("name")!).ToHashSet();

        var keys = Regex.Matches(source, @"T\[""((?:[^""\\]|\\.)+)""")
            .Select(match => match.Groups[1].Value)
            .Concat(Regex.Matches(source, @"T\.Plural\([^,]+,\s*""([^""]+)"",\s*""([^""]+)""")
                .SelectMany(match => new[] { match.Groups[1].Value, match.Groups[2].Value }))
            .Distinct();

        Assert.Empty(keys.Where(key => !translated.Contains(key)));
    }

    private static string FindWebProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Web", "Web.csproj")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "Web");
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
