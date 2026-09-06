using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

// Mesma ressalva de KeetaEventPollingBackgroundServiceTests: ExecuteAsync tem delays fixos (10s +
// 30s) sem seam de tempo injetável — RunCycleAsync (a lógica de negócio real) é testado via
// reflection; ExecuteAsync só tem seu caminho de cancelamento imediato coberto.
public sealed class IfoodOrderPollingBackgroundServiceTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<IfoodOrderPollingBackgroundService> _logger = NullLogger<IfoodOrderPollingBackgroundService>.Instance;
    private readonly IfoodOrderPollingBackgroundService _service;

    public IfoodOrderPollingBackgroundServiceTests()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IIfoodIntegrationSettingRepository)).Returns(_settingRepository);
        scopedProvider.GetService(typeof(IMediator)).Returns(_mediator);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _service = new IfoodOrderPollingBackgroundService(rootProvider, _logger);
    }

    private Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var method = typeof(IfoodOrderPollingBackgroundService).GetMethod("RunCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(_service, [cancellationToken])!;
    }

    [Fact]
    public async Task RunCycleAsync_NoEnabledCompanies_ShouldNotDispatchAnyCommand()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[]);

        await RunCycleAsync();

        await _mediator.DidNotReceive().Send(Arg.Any<SyncIfoodOrdersCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_MultipleCompanies_ShouldDispatchOneCommandPerCompany()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[1, 2]);

        await RunCycleAsync();

        await _mediator.Received(1).Send(Arg.Is<SyncIfoodOrdersCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(Arg.Is<SyncIfoodOrdersCommand>(c => c.CompanyId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_OneCompanyThrows_ShouldLogAndStillDispatchRemaining()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[1, 2]);
        _mediator.Send(Arg.Is<SyncIfoodOrdersCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        await _mediator.Received(1).Send(Arg.Is<SyncIfoodOrdersCommand>(c => c.CompanyId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ShouldReturnWithoutDoingAnyWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var method = typeof(IfoodOrderPollingBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var act = () => (Task)method.Invoke(_service, [cts.Token])!;

        await act.Should().NotThrowAsync();
        await _settingRepository.DidNotReceive().GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>());
    }
}
