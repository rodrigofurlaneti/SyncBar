using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Keeta;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

// KeetaEventPollingBackgroundService.ExecuteAsync tem um delay inicial fixo de 10s e um intervalo
// de 30s entre ciclos (Task.Delay direto, sem TimeProvider injetável) — esperar por eles de
// verdade deixaria a suíte lenta sem cobrir lógica de negócio nova. RunCycleAsync (onde mora toda
// a lógica: agrupamento por tenant, disparo do comando por escopo, isolamento de falha por
// escopo) é testado diretamente via reflection, já que é privado.
public sealed class KeetaEventPollingBackgroundServiceTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<KeetaEventPollingBackgroundService> _logger = NullLogger<KeetaEventPollingBackgroundService>.Instance;
    private readonly KeetaEventPollingBackgroundService _service;

    public KeetaEventPollingBackgroundServiceTests()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IKeetaIntegrationMerchantMappingRepository)).Returns(_mappingRepository);
        scopedProvider.GetService(typeof(IMediator)).Returns(_mediator);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _service = new KeetaEventPollingBackgroundService(rootProvider, _logger);
    }

    private static KeetaIntegrationMerchantMapping CreateMapping(long companyId, long branchId, string internalMerchantId) =>
        KeetaIntegrationMerchantMapping.Create(companyId, branchId, internalMerchantId, keetaMerchantId: 1, storeName: "Loja").Value;

    private Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var method = typeof(KeetaEventPollingBackgroundService).GetMethod("RunCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(_service, [cancellationToken])!;
    }

    [Fact]
    public async Task RunCycleAsync_NoAuthorizedMappings_ShouldNotDispatchAnyCommand()
    {
        _mappingRepository.GetAllAuthorizedAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<KeetaIntegrationMerchantMapping>)[]);

        await RunCycleAsync();

        await _mediator.DidNotReceive().Send(Arg.Any<PollKeetaEventsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_SingleTenantWithMultipleMerchants_ShouldDispatchOneCommandWithAllMerchantIds()
    {
        _mappingRepository.GetAllAuthorizedAsync(Arg.Any<CancellationToken>()).Returns(
            (IReadOnlyList<KeetaIntegrationMerchantMapping>)
            [
                CreateMapping(1, 10, "MERCH-A"),
                CreateMapping(1, 10, "MERCH-B"),
            ]);

        await RunCycleAsync();

        await _mediator.Received(1).Send(
            Arg.Is<PollKeetaEventsCommand>(c => c.CompanyId == 1 && c.BranchId == 10 && c.MerchantIds.SequenceEqual(new[] { "MERCH-A", "MERCH-B" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_MultipleTenantScopes_ShouldDispatchOneCommandPerScope()
    {
        _mappingRepository.GetAllAuthorizedAsync(Arg.Any<CancellationToken>()).Returns(
            (IReadOnlyList<KeetaIntegrationMerchantMapping>)
            [
                CreateMapping(1, 10, "MERCH-A"),
                CreateMapping(2, 20, "MERCH-C"),
            ]);

        await RunCycleAsync();

        await _mediator.Received(1).Send(Arg.Is<PollKeetaEventsCommand>(c => c.CompanyId == 1 && c.BranchId == 10), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(Arg.Is<PollKeetaEventsCommand>(c => c.CompanyId == 2 && c.BranchId == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_OneScopeThrows_ShouldLogAndStillDispatchRemainingScopes()
    {
        _mappingRepository.GetAllAuthorizedAsync(Arg.Any<CancellationToken>()).Returns(
            (IReadOnlyList<KeetaIntegrationMerchantMapping>)
            [
                CreateMapping(1, 10, "MERCH-A"),
                CreateMapping(2, 20, "MERCH-C"),
            ]);
        _mediator.Send(Arg.Is<PollKeetaEventsCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        await _mediator.Received(1).Send(Arg.Is<PollKeetaEventsCommand>(c => c.CompanyId == 2 && c.BranchId == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ShouldReturnWithoutDoingAnyWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var method = typeof(KeetaEventPollingBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var act = () => (Task)method.Invoke(_service, [cts.Token])!;

        await act.Should().NotThrowAsync();
        await _mappingRepository.DidNotReceive().GetAllAuthorizedAsync(Arg.Any<CancellationToken>());
    }
}
