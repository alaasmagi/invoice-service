using Application;
using Domain;
using Xunit;

namespace Application.Tests;

public sealed class PdfInvoiceMatchingTests
{
    [Fact]
    public void ServiceMatchingAllowsProviderNameInsideServiceName()
    {
        var service = PdfInvoiceImportService.FindService([
            new Service { Id = Guid.NewGuid(), Name = "Telia IKT teenused" }
        ], "Telia");

        Assert.NotNull(service);
    }

    [Fact]
    public void AddressMatchingUsesNameAndFullAddress()
    {
        var addressId = Guid.NewGuid();
        var address = PdfInvoiceImportService.FindAddress([
            new Address { Id = addressId, Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" }
        ], "Tiiu talu, Leesi");

        Assert.NotNull(address);
        Assert.Equal(addressId, address.Id);
    }

    [Fact]
    public void AddressMatchingAllowsPartialDbAddressMatch()
    {
        var addressId = Guid.NewGuid();
        var address = PdfInvoiceImportService.FindAddress([
            new Address { Id = addressId, Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" }
        ], "Leesi");

        Assert.NotNull(address);
        Assert.Equal(addressId, address.Id);
    }

    [Fact]
    public void AddressMatchingRejectsAmbiguousPartialMatches()
    {
        var address = PdfInvoiceImportService.FindAddress([
            new Address { Id = Guid.NewGuid(), Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" },
            new Address { Id = Guid.NewGuid(), Name = "Leesi - Mari", FullAddress = "Mari talu, Leesi" }
        ], "Leesi");

        Assert.Null(address);
    }

    [Fact]
    public void AddressMatchingAllowsOfficialAddressTextTokenMatch()
    {
        var addressId = Guid.NewGuid();
        var address = PdfInvoiceImportService.FindAddress([
            new Address { Id = addressId, Name = "Leesi - Tiiu", FullAddress = "Tiiu talu, Leesi" }
        ], "HARJU MAAKOND KUUSALU VALD LEESI");

        Assert.NotNull(address);
        Assert.Equal(addressId, address.Id);
    }
}
