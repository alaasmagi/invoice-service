using Application;
using Contracts.Application;
using DataAccess;
using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests;

public sealed class PdfInvoiceImportServiceTests
{
    [Fact]
    public async Task ImportCreatesMatchedRowsAndReportsUnmatchedRows()
    {
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Services.Add(TestEntityStamp.Stamp(new ServiceEntity { Id = serviceId, UserId = userId, Name = "Telia IKT teenused" }, userId));
        context.Addresses.Add(TestEntityStamp.Stamp(new AddressEntity { Id = addressId, UserId = userId, Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" }, userId));
        await context.SaveChangesAsync();

        var service = CreateService(context, new StubParser([
            new PdfInvoiceParsedRow { AddressText = "Leesi - Tiiu", Amount = 40m, InvoiceDate = new DateOnly(2026, 6, 10), PeriodStart = new DateOnly(2026, 5, 1), PeriodEnd = new DateOnly(2026, 5, 31) },
            new PdfInvoiceParsedRow { AddressText = "Unknown address", Amount = 15m, InvoiceDate = new DateOnly(2026, 6, 10), PeriodStart = new DateOnly(2026, 5, 1), PeriodEnd = new DateOnly(2026, 5, 31) }
        ]));

        var result = await service.ImportAsync(Request(), userId);

        Assert.Empty(result.FatalErrors);
        Assert.Single(result.CreatedInvoices);
        Assert.Single(result.SkippedRows);
        Assert.Equal("No matching address was found.", result.SkippedRows[0].Reason);
    }

    [Fact]
    public async Task ImportReportsDuplicateRowsAsSkipped()
    {
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Services.Add(TestEntityStamp.Stamp(new ServiceEntity { Id = serviceId, UserId = userId, Name = "Telia IKT teenused" }, userId));
        context.Addresses.Add(TestEntityStamp.Stamp(new AddressEntity { Id = addressId, UserId = userId, Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" }, userId));
        context.Invoices.Add(TestEntityStamp.Stamp(new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            AddressId = addressId,
            InvoiceDate = new DateOnly(2026, 6, 10),
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            TotalSum = 40m
        }, userId));
        await context.SaveChangesAsync();

        var service = CreateService(context, new StubParser([
            new PdfInvoiceParsedRow { AddressText = "Leesi - Tiiu", Amount = 40m, InvoiceDate = new DateOnly(2026, 6, 10), PeriodStart = new DateOnly(2026, 5, 1), PeriodEnd = new DateOnly(2026, 5, 31) }
        ]));

        var result = await service.ImportAsync(Request(), userId);

        Assert.Empty(result.FatalErrors);
        Assert.Empty(result.CreatedInvoices);
        var skipped = Assert.Single(result.SkippedRows);
        Assert.Equal("An equivalent invoice already exists.", skipped.Reason);
    }

    private static PdfInvoiceImportService CreateService(AppDbContext context, IPdfInvoiceParser parser)
    {
        var invoiceRepository = new InvoiceRepository(context, new InvoiceEntityMapper());
        return new PdfInvoiceImportService(
            new StubExtractor(),
            new StubDetector(),
            [parser],
            new ServiceRepository(context, new ServiceEntityMapper()),
            new AddressRepository(context, new AddressEntityMapper()),
            invoiceRepository,
            new CreateInvoiceService(
                invoiceRepository,
                new AddressContactRepository(context, new AddressContactEntityMapper()),
                new InvoiceAllocationRepository(context, new InvoiceAllocationEntityMapper()),
                new DataAccessUow(context)));
    }

    private static PdfInvoiceImportRequest Request()
    {
        return new PdfInvoiceImportRequest
        {
            FileName = "invoice.pdf",
            Length = 10,
            PdfStream = new MemoryStream("Telia"u8.ToArray())
        };
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubExtractor : IPdfInvoiceTextExtractor
    {
        public Task<string> ExtractTextAsync(Stream pdfStream) => Task.FromResult("Telia");
    }

    private sealed class StubDetector : IPdfInvoiceProviderDetector
    {
        public PdfInvoiceProviderDetectionResult Detect(string text) => PdfInvoiceProviderDetectionResult.Detected(EPdfInvoiceProvider.Telia);
    }

    private sealed class StubParser(IReadOnlyCollection<PdfInvoiceParsedRow> rows) : IPdfInvoiceParser
    {
        public EPdfInvoiceProvider Provider => EPdfInvoiceProvider.Telia;

        public PdfInvoiceParsedDocument Parse(string text)
        {
            var document = new PdfInvoiceParsedDocument
            {
                Provider = EPdfInvoiceProvider.Telia,
                ServiceName = "Telia"
            };
            document.Rows.AddRange(rows);
            return document;
        }
    }
}
