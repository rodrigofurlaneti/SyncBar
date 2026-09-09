using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Ifood;

public sealed class IfoodWebhookTests : RepositoryTestBase
{
    private const string Secret = "test-only-secret";
    private const string Payload = """{"id":"event-1","code":"PLC","fullCode":"PLACED","orderId":"order-1","merchantId":"merchant-1","createdAt":"2026-09-08T12:00:00Z"}""";
    private readonly IIfoodIntegrationSettingRepository _settings = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappings = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly ISecretProtector _protector = Substitute.For<ISecretProtector>();
    private readonly IfoodEventInboxStore _inbox;
    private readonly IfoodWebhookReceiver _receiver;
    private readonly IfoodIntegrationSetting _setting;

    public IfoodWebhookTests()
    {
        _setting = IfoodIntegrationSetting.Create(1).Value;
        _setting.SaveCredentials("client", "encrypted", true, null);
        _setting.SetEventDeliveryMode("Webhook");
        _settings.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(_setting);
        _settings.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns(new long[] { 1 });
        _protector.Unprotect("SyncBar.Integrations.Ifood.ClientSecret.v1", "encrypted").Returns(Secret);
        var mapping = IfoodMerchantMapping.Create(10).Value;
        mapping.SetMerchant("merchant-1", "merchant-1");
        _mappings.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(new Dictionary<long, IfoodMerchantMapping> { [10] = mapping });
        _inbox = new IfoodEventInboxStore(Context, TimeProvider.System);
        _receiver = new IfoodWebhookReceiver(_settings, _mappings, _protector, _inbox);
    }

    private static string Sign(string body) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body)));
    private Task<IfoodWebhookReceipt> Receive(string body) => _receiver.ReceiveAsync(1, Encoding.UTF8.GetBytes(body), Sign(body), default);

    [Fact]
    public async Task SharedEndpoint_ResolvesCompanyAndDeduplicates()
    {
        for (var i = 0; i < 2; i++)
            (await _receiver.ReceiveAsync(Encoding.UTF8.GetBytes(Payload), Sign(Payload), default)).StatusCode.Should().Be(202);
        var entry = await Context.Set<IfoodEventInbox>().SingleAsync();
        entry.CompanyId.Should().Be(1);
    }

    [Theory]
    [InlineData("merchant-1", false, 401)]
    [InlineData("foreign", true, 403)]
    public async Task SharedEndpoint_RejectsInvalidSignatureOrForeignMerchant(string merchant, bool validSignature, int status)
    {
        var payload = Payload.Replace("merchant-1", merchant);
        (await _receiver.ReceiveAsync(Encoding.UTF8.GetBytes(payload), validSignature ? Sign(payload) : new string('0', 64), default))
            .StatusCode.Should().Be(status);
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SharedEndpoint_AmbiguousMerchantDoesNotEnqueue()
    {
        var other = IfoodIntegrationSetting.Create(2).Value;
        other.SaveCredentials("client", "encrypted", true, null);
        other.SetEventDeliveryMode("Webhook");
        _settings.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns(new long[] { 1, 2 });
        _settings.GetByCompanyAsync(2, Arg.Any<CancellationToken>()).Returns(other);
        var sharedMappings = await _mappings.GetByCompanyAsync(1);
        _mappings.GetByCompanyAsync(2, Arg.Any<CancellationToken>()).Returns(sharedMappings);
        (await _receiver.ReceiveAsync(Encoding.UTF8.GetBytes(Payload), Sign(Payload), default)).StatusCode.Should().Be(503);
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SharedEndpoint_PresenceFiltersMerchants()
    {
        const string payload = """{"code":"KEEPALIVE","merchantIds":["merchant-1","foreign"]}""";
        var receipt = await _receiver.ReceiveAsync(Encoding.UTF8.GetBytes(payload), Sign(payload), default);
        receipt.StatusCode.Should().Be(202);
        receipt.MerchantIds.Should().Equal("merchant-1");
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ValidSignature_PersistsBeforeAcceptingAndDeduplicatesAcrossTransports()
    {
        (await Receive(Payload)).StatusCode.Should().Be(202);
        Context.ChangeTracker.Clear();
        (await Context.Set<IfoodEventInbox>().SingleAsync()).Payload.Should().Be(Payload);
        (await Receive(Payload)).StatusCode.Should().Be(202);
        await _inbox.EnqueueAsync(1, "event-1", "polling duplicate", default);
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public async Task InvalidSignature_DoesNotPersist(string? signature)
    {
        (await _receiver.ReceiveAsync(1, Encoding.UTF8.GetBytes(Payload), signature, default)).StatusCode.Should().Be(401);
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Signature_UsesExactBodyBytes()
    {
        IfoodWebhookReceiver.Verify(Encoding.UTF8.GetBytes(Payload), Secret, Sign(Payload)).Should().BeTrue();
        IfoodWebhookReceiver.Verify(Encoding.UTF8.GetBytes(Payload + " "), Secret, Sign(Payload)).Should().BeFalse();
    }

    [Fact]
    public async Task PollingMode_RejectsWebhookAndPresence()
    {
        _setting.SetEventDeliveryMode("Polling");
        (await Receive(Payload)).StatusCode.Should().Be(503);
        (await Receive("""{"code":"KEEPALIVE","id":"ping"}""")).StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task Presence_OnlyReturnsMappedMerchantsAndDoesNotQueueHeartbeat()
    {
        var response = await Receive("""{"code":"KEEPALIVE","merchantIds":["merchant-1","foreign-merchant"]}""");
        response.StatusCode.Should().Be(202);
        response.MerchantIds.Should().Equal("merchant-1");
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ForeignMerchantAndMalformedJson_AreRejected()
    {
        (await Receive(Payload.Replace("merchant-1", "foreign"))).StatusCode.Should().Be(403);
        (await Receive("{broken")).StatusCode.Should().Be(400);
        (await Context.Set<IfoodEventInbox>().CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Worker_PersistsSuccessOrSchedulesRetry(bool succeeds)
    {
        await Receive(Payload);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SyncIfoodOrdersCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds ? Result.Success() : Result.Failure(new Error("Test.Failure", "Falha")));
        using var services = new ServiceCollection().AddSingleton(Context).AddSingleton(mediator).BuildServiceProvider();
        var worker = new IfoodEventInboxBackgroundService(services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System,
            NullLogger<IfoodEventInboxBackgroundService>.Instance);
        await worker.ProcessAsync(default);
        await worker.ProcessAsync(default);
        var row = await Context.Set<IfoodEventInbox>().AsNoTracking().SingleAsync();
        row.Attempts.Should().Be(1);
        row.ProcessedAtUtc.HasValue.Should().Be(succeeds);
        row.LastError.Should().Be(succeeds ? null : "Test.Failure");
        await mediator.Received(1).Send(Arg.Is<SyncIfoodOrdersCommand>(cmd => cmd.CompanyId == 1 && cmd.ReceivedEvents != null && cmd.ReceivedEvents.Count == 1), Arg.Any<CancellationToken>());
    }
}
