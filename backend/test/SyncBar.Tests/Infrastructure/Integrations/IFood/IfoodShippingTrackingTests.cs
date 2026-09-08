using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Ifood;

public sealed class IfoodShippingTrackingTests : RepositoryTestBase
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private readonly Clock _time = new();

    private async Task SeedOrderAsync()
    {
        var branch = Branch.Create(1, "Filial", null, null, null, null, null, null, null, null).Value;
        Context.Add(branch);
        await Context.SaveChangesAsync();
        Context.Add(IfoodOrder.Create(1, branch.Id, "order-1", "001", "merchant", "DELIVERY", "MERCHANT",
            "IMMEDIATE", null, _time.Now.UtcDateTime, false).Value);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Assignment_IsDurableIdempotentAndScoped()
    {
        await SeedOrderAsync();
        var store = new IfoodShippingTrackingStore(Context, _time);
        var evt = new IfoodPollingEvent("event-1", "ASSIGN_DRIVER", null, "order-1", _time.Now.UtcDateTime);
        (await store.ApplyEventAsync(2, evt, default)).Should().BeFalse();
        (await store.ApplyEventAsync(1, evt, default)).Should().BeTrue();
        (await store.ApplyEventAsync(1, evt, default)).Should().BeTrue();
        (await Context.Set<IfoodShippingTracking>().CountAsync()).Should().Be(1);
        Context.ChangeTracker.Clear();
        (await Context.Set<IfoodShippingTracking>().SingleAsync()).IsActive.Should().BeTrue();
        await store.ApplyEventAsync(1, evt with { Id = "event-2", Code = "DELIVERY_CONCLUDED" }, default);
        Context.ChangeTracker.Clear();
        (await Context.Set<IfoodShippingTracking>().SingleAsync()).IsActive.Should().BeFalse();
        await store.ApplyEventAsync(1, evt, default);
        Context.ChangeTracker.Clear();
        (await Context.Set<IfoodShippingTracking>().SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Polling_WaitsForAssignmentAndRespectsThirtySecondsAcrossWorkerInstances()
    {
        await SeedOrderAsync();
        var client = Substitute.For<IIfoodShippingClient>();
        var tokens = Substitute.For<IIfoodTokenProvider>();
        tokens.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns("token");
        client.GetTrackingAsync("token", "order-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingTrackingResult(true, null, -23, -46, null, null, null));
        using var services = new ServiceCollection().AddSingleton(Context).AddSingleton(tokens).AddSingleton(client).BuildServiceProvider();
        var scopes = services.GetRequiredService<IServiceScopeFactory>();
        var worker = new IfoodShippingTrackingBackgroundService(scopes, _time, NullLogger<IfoodShippingTrackingBackgroundService>.Instance);
        var otherWorker = new IfoodShippingTrackingBackgroundService(scopes, _time, NullLogger<IfoodShippingTrackingBackgroundService>.Instance);
        await worker.PollAsync(default);
        await client.DidNotReceiveWithAnyArgs().GetTrackingAsync(default!, default!, default);

        var store = new IfoodShippingTrackingStore(Context, _time);
        await store.ApplyEventAsync(1, new("1", "ASSIGN_DRIVER", null, "order-1", _time.Now.UtcDateTime), default);
        await worker.PollAsync(default);
        await otherWorker.PollAsync(default);
        _time.Now = _time.Now.AddSeconds(29);
        await otherWorker.PollAsync(default);
        await client.Received(1).GetTrackingAsync("token", "order-1", Arg.Any<CancellationToken>());
        _time.Now = _time.Now.AddSeconds(1);
        await otherWorker.PollAsync(default);
        await client.Received(2).GetTrackingAsync("token", "order-1", Arg.Any<CancellationToken>());
        (await store.ReadAsync(1, "order-1", default)).Latitude.Should().Be(-23);
        (await store.ReadAsync(2, "order-1", default)).Latitude.Should().BeNull();
        await store.ApplyEventAsync(1, new("2", "CANCELLED", null, "order-1", _time.Now.UtcDateTime), default);
        _time.Now = _time.Now.AddMinutes(1);
        await worker.PollAsync(default);
        await client.Received(2).GetTrackingAsync("token", "order-1", Arg.Any<CancellationToken>());
    }
}
