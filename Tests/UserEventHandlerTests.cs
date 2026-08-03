using Base.Keycloak.Events;
using Base.Message;
using Contracts.DataAccess;
using Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public sealed class UserEventHandlerTests
{
    private static BaseEventEnvelope<UserEventContent> Envelope(
        string source = "identity-hub",
        string tenant = "realmName/appName",
        string action = "user-created",
        string contentVersion = "1.0",
        string? userId = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Source = source,
        Tenant = tenant,
        Action = action,
        Timestamp = "2026-07-08T12:44:37.408Z",
        ContentVersion = contentVersion,
        Content = new UserEventContent
        {
            UserId = userId ?? Guid.NewGuid().ToString(),
            Email = "u@example.com",
            Username = "u",
            FullName = "U",
            Locale = "et"
        }
    };

    [Fact]
    public async Task Handle_RejectsForeignSource_WithoutProjecting()
    {
        var projection = new Mock<IIdentityUserProjection>();
        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(Envelope(source: "educode"), CancellationToken.None));

        projection.Verify(p => p.TryProjectAsync(It.IsAny<IdentityUserProjectionInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsMissingTenant()
    {
        var projection = new Mock<IIdentityUserProjection>();
        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(Envelope(tenant: " "), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RejectsUnsupportedMajorVersion()
    {
        var projection = new Mock<IIdentityUserProjection>();
        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(Envelope(contentVersion: "2.0"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AcceptsMinorVersionBump_AndProjects()
    {
        var projection = new Mock<IIdentityUserProjection>();
        projection.Setup(p => p.TryProjectAsync(It.IsAny<IdentityUserProjectionInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        // A consumer written for "1.0" accepts "1.5" (major matches).
        await handler.HandleAsync(Envelope(contentVersion: "1.5"), CancellationToken.None);

        projection.Verify(p => p.TryProjectAsync(It.IsAny<IdentityUserProjectionInput>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateDelivery_AcksWithoutThrowing()
    {
        var projection = new Mock<IIdentityUserProjection>();
        projection.Setup(p => p.TryProjectAsync(It.IsAny<IdentityUserProjectionInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // already processed
        var handler = new UserEventHandler(ScopeFactoryFor(projection.Object), NullLogger<UserEventHandler>.Instance);

        // Must not throw — a throw would nack and dead-letter a harmless duplicate.
        await handler.HandleAsync(Envelope(), CancellationToken.None);
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
