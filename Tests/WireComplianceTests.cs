using Base.Keycloak.Events;
using Base.Message;
using Contracts.Application;
using Contracts.DataAccess;
using Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Tests;

/// <summary>
/// Verifies the service against the exact wire payloads observed on the broker (source "identity-hub",
/// tenant "realmName/appName", nanosecond timestamps). Deserialization mirrors the shared consumer
/// (default options; envelope/content property names come from their JsonPropertyName attributes).
/// </summary>
public class WireComplianceTests
{
    private const string UserCreated = """
    {"id":"2b9dc316-98d7-488f-8614-6d275497d0b9","source":"identity-hub","tenant":"realmName/appName","action":"user-created","timestamp":"2026-07-08T12:44:37.408804148Z","contentVersion":"1.0","content":{"userId":"1b9dc316-98d7-488f-8614-6d275497d0b9","email":"user@example.com","username":"testUser","fullName":"testFullName","locale":"et"}}
    """;

    private const string UserUpdated = """
    {"id":"2b9dc316-98d7-488f-8614-6d275497d0b9","source":"identity-hub","tenant":"realmName/appName","action":"user-updated","timestamp":"2026-07-08T12:44:37.408804148Z","contentVersion":"1.0","content":{"userId":"1b9dc316-98d7-488f-8614-6d275497d0b9","email":"user@example.com","username":"testUser","fullName":"testFullName","locale":"et"}}
    """;

    private const string UserEnabled = """
    {"id":"2b9dc316-98d7-488f-8614-6d275497d0b9","source":"identity-hub","tenant":"realmName/appName","action":"user-enabled","timestamp":"2026-07-08T12:44:37.408804148Z","contentVersion":"1.0","content":{"userId":"1b9dc316-98d7-488f-8614-6d275497d0b9","email":"user@example.com","username":"testUser","fullName":"testFullName","locale":"et"}}
    """;

    private const string UserDisabled = """
    {"id":"2b9dc316-98d7-488f-8614-6d275497d0b9","source":"identity-hub","tenant":"realmName/appName","action":"user-disabled","timestamp":"2026-07-08T12:44:37.408804148Z","contentVersion":"1.0","content":{"userId":"1b9dc316-98d7-488f-8614-6d275497d0b9","email":"user@example.com","username":"testUser","fullName":"testFullName","locale":"et"}}
    """;

    private const string UserDeleted = """
    {"id":"2b9dc316-98d7-488f-8614-6d275497d0b9","source":"identity-hub","tenant":"realmName/appName","action":"user-deleted","timestamp":"2026-07-08T12:44:37.408804148Z","contentVersion":"1.0","content":{"userId":"65d43b2d-2f0d-4eaa-8a40-a3f6c2698bbf"}}
    """;

    [Theory]
    [InlineData(UserCreated, "user-created", "1b9dc316-98d7-488f-8614-6d275497d0b9")]
    [InlineData(UserUpdated, "user-updated", "1b9dc316-98d7-488f-8614-6d275497d0b9")]
    [InlineData(UserEnabled, "user-enabled", "1b9dc316-98d7-488f-8614-6d275497d0b9")]
    [InlineData(UserDisabled, "user-disabled", "1b9dc316-98d7-488f-8614-6d275497d0b9")]
    [InlineData(UserDeleted, "user-deleted", "65d43b2d-2f0d-4eaa-8a40-a3f6c2698bbf")]
    public async Task RealUserEvent_DeserializesValidatesAndProjects(string json, string expectedAction, string expectedUserId)
    {
        var envelope = JsonSerializer.Deserialize<BaseEventEnvelope<UserEventContent>>(json);
        Assert.NotNull(envelope);

        IdentityUserProjectionInput? captured = null;
        var projection = new Mock<IIdentityUserProjection>();
        projection
            .Setup(p => p.TryProjectAsync(It.IsAny<IdentityUserProjectionInput>(), It.IsAny<CancellationToken>()))
            .Callback<IdentityUserProjectionInput, CancellationToken>((input, _) => captured = input)
            .ReturnsAsync(true);

        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        await handler.HandleAsync(envelope!, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("identity-hub", captured!.Source);
        Assert.Equal(expectedAction, captured.Action);
        Assert.Equal(Guid.Parse(expectedUserId), captured.UserId);
    }

    [Fact]
    public void InvoicePublishContent_RoundTripsRealSample_ByteForByte()
    {
        const string sampleContent = """
        {"email":"user@example.com","fullName":"testFullName","locale":"et","invoiceIdentifier":"1D58MKI","invoicePeriod":"2026-06","invoiceDate":"2026-06-06","invoiceDueDate":"2026-06-20","invoiceTotal":"97.87","currency":"EUR","invoiceRows":[{"address":"Leesi - Tiiu","service":"Enefit elektriteenus","periodStart":"2026-05-01","periodEnd":"2026-05-31","invoiceAmount":"75.75","fractions":1,"invoiceFraction":"75.75"},{"address":"Tallinn - Tehnika","service":"Telia IKT teenused","periodStart":"2026-05-01","periodEnd":"2026-05-31","invoiceAmount":"40.00","fractions":2,"invoiceFraction":"20.00"}],"paymentFullName":"Aleksander Laasmägi","paymentIban":"EE621010011679940224","paymentDescription":"1D58MKI 06.2026"}
        """;

        // The model this service emits must round-trip the real content with no added, missing or
        // renamed field and no changed value/type (e.g. fractions stays a number, amounts stay strings).
        var content = JsonSerializer.Deserialize<MonthlyInvoiceEmailContent>(sampleContent);
        Assert.NotNull(content);

        Assert.Equal(Canonical(sampleContent), Canonical(JsonSerializer.Serialize(content)));
    }

    private static string Canonical(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static IServiceScopeFactory ScopeFactoryFor(IIdentityUserProjection projection)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IIdentityUserProjection))).Returns(projection);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        return factory.Object;
    }
}
