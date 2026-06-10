using DataAccess;
using DataAccess.Context;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests;

public sealed class InvoiceRepositoryDuplicateTests
{
    [Fact]
    public async Task ExistsEquivalentFindsCurrentUserInvoice()
    {
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await using var context = CreateContext();
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

        var repository = new InvoiceRepository(context, new InvoiceEntityMapper());
        var exists = await repository.ExistsEquivalentAsync(
            userId,
            serviceId,
            addressId,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            40m);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsEquivalentIgnoresAnotherUserInvoice()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Invoices.Add(TestEntityStamp.Stamp(new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            ServiceId = serviceId,
            AddressId = addressId,
            InvoiceDate = new DateOnly(2026, 6, 10),
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            TotalSum = 40m
        }, otherUserId));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context, new InvoiceEntityMapper());
        var exists = await repository.ExistsEquivalentAsync(
            currentUserId,
            serviceId,
            addressId,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            40m);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsEquivalentRequiresSameAmount()
    {
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await using var context = CreateContext();
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

        var repository = new InvoiceRepository(context, new InvoiceEntityMapper());
        var exists = await repository.ExistsEquivalentAsync(
            userId,
            serviceId,
            addressId,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            41m);

        Assert.False(exists);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
