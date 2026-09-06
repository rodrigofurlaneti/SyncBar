using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

// Mesma ressalva das demais suítes de BackgroundService: delays fixos (2min + 24h) tornam
// ExecuteAsync impraticável de testar via timing real — RunCycleAsync (que concentra toda a
// lógica: sync por empresa + checagem de discrepância por filial com detecção de transição) é
// invocado via reflection. _lastKnownHasDiscrepancy é estado de instância, então os testes de
// transição chamam RunCycleAsync duas vezes na MESMA instância de serviço.
public sealed class IfoodFinancialSyncBackgroundServiceTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodOperationalAlertStore _alertStore = Substitute.For<IIfoodOperationalAlertStore>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<IfoodFinancialSyncBackgroundService> _logger = NullLogger<IfoodFinancialSyncBackgroundService>.Instance;
    private readonly IfoodFinancialSyncBackgroundService _service;

    public IfoodFinancialSyncBackgroundServiceTests()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IIfoodIntegrationSettingRepository)).Returns(_settingRepository);
        scopedProvider.GetService(typeof(IIfoodMerchantMappingRepository)).Returns(_mappingRepository);
        scopedProvider.GetService(typeof(IBranchRepository)).Returns(_branchRepository);
        scopedProvider.GetService(typeof(IIfoodOperationalAlertStore)).Returns(_alertStore);
        scopedProvider.GetService(typeof(IMediator)).Returns(_mediator);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _service = new IfoodFinancialSyncBackgroundService(rootProvider, _logger);

        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[1]);
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping>());
    }

    private Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var method = typeof(IfoodFinancialSyncBackgroundService).GetMethod("RunCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(_service, [cancellationToken])!;
    }

    private static IfoodMerchantMapping CreateMapping(long branchId, string? merchantId = "MERCH-1")
    {
        var mapping = IfoodMerchantMapping.Create(branchId).Value;
        if (merchantId is not null)
            mapping.SetMerchant(merchantId, "uuid-1");
        return mapping;
    }

    private void SetupSummary(long branchId, bool hasDiscrepancy, decimal amount = 10m)
    {
        _mediator.Send(Arg.Is<GetIfoodFinancialSummaryQuery>(q => q.BranchId == branchId), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodFinancialSummaryResponse(
                DateTime.Today.AddDays(-30), DateTime.Today, 100m, hasDiscrepancy ? 100m - amount : 100m, hasDiscrepancy, amount, [], [])));
    }

    [Fact]
    public async Task RunCycleAsync_NoEnabledCompanies_ShouldNotDispatchAnyCommand()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[]);

        await RunCycleAsync();

        await _mediator.DidNotReceive().Send(Arg.Any<SyncIfoodFinancialCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_SyncSucceeds_ShouldDispatchFinancialSyncCommand()
    {
        await RunCycleAsync();

        await _mediator.Received(1).Send(Arg.Is<SyncIfoodFinancialCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_SyncFails_ShouldSkipDiscrepancyCheckAndNotThrow()
    {
        _mediator.Send(Arg.Any<SyncIfoodFinancialCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        await _mappingRepository.DidNotReceive().GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_MappingWithBlankMerchantId_ShouldSkipDiscrepancyCheckForThatBranch()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping>
            {
                [10] = CreateMapping(10, merchantId: null),
            });

        await RunCycleAsync();

        await _mediator.DidNotReceive().Send(Arg.Any<GetIfoodFinancialSummaryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_SummaryQueryFails_ShouldNotRaiseAlert()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        _mediator.Send(Arg.Any<GetIfoodFinancialSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodFinancialSummaryResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_FirstCycleWithDiscrepancy_ShouldOnlyRecordBaselineWithoutAlerting()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        SetupSummary(10, hasDiscrepancy: true);

        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_DiscrepancyPersistsAcrossCycles_ShouldNotAlertAgain()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        SetupSummary(10, hasDiscrepancy: true);

        await RunCycleAsync();
        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToDiscrepancy_ShouldRaiseWarningAlertWithBranchNameAndAmount()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        var branch = Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value;
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(branch);

        SetupSummary(10, hasDiscrepancy: false);
        await RunCycleAsync();

        SetupSummary(10, hasDiscrepancy: true, amount: 55.5m);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Is<string>(t => t.Contains("Discrepância")), Arg.Is<string>(m => m.Contains("55,50") || m.Contains("55.50")), IfoodOperationalAlertSeverity.Warning);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToResolved_ShouldRaiseInfoAlert()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        var branch = Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value;
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(branch);

        SetupSummary(10, hasDiscrepancy: true);
        await RunCycleAsync();

        SetupSummary(10, hasDiscrepancy: false);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Is<string>(t => t.Contains("resolvida")), Arg.Any<string>(), IfoodOperationalAlertSeverity.Info);
    }

    [Fact]
    public async Task RunCycleAsync_BranchNotFound_ShouldUseFallbackBranchName()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        SetupSummary(10, hasDiscrepancy: false);
        await RunCycleAsync();

        SetupSummary(10, hasDiscrepancy: true);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Filial 10", Arg.Any<string>(), Arg.Any<string>(), IfoodOperationalAlertSeverity.Warning);
    }

    [Fact]
    public async Task RunCycleAsync_MappingRepositoryThrows_ShouldLogAndNotCrash()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha ao carregar mapeamentos"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_SummaryQueryThrows_ShouldLogAndNotCrash()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [10] = CreateMapping(10) });
        _mediator.Send(Arg.Any<GetIfoodFinancialSummaryQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ShouldReturnWithoutDoingAnyWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var method = typeof(IfoodFinancialSyncBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var act = () => (Task)method.Invoke(_service, [cts.Token])!;

        await act.Should().NotThrowAsync();
        await _settingRepository.DidNotReceive().GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>());
    }
}
