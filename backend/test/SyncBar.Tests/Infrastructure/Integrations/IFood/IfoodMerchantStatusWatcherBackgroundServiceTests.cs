using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

// Mesma ressalva das demais suítes de BackgroundService quanto a ExecuteAsync (delays reais de
// 45s + 5min) — RunCycleAsync é invocado via reflection. _lastKnownAvailable e
// _unavailableWasExpectedClosure são estado de instância: testes de transição chamam RunCycleAsync
// mais de uma vez na MESMA instância.
public sealed class IfoodMerchantStatusWatcherBackgroundServiceTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly IIfoodOperationalAlertStore _alertStore = Substitute.For<IIfoodOperationalAlertStore>();
    private readonly IIfoodOpeningHoursRepository _openingHoursRepository = Substitute.For<IIfoodOpeningHoursRepository>();
    private readonly ILogger<IfoodMerchantStatusWatcherBackgroundService> _logger = NullLogger<IfoodMerchantStatusWatcherBackgroundService>.Instance;
    private readonly IfoodMerchantStatusWatcherBackgroundService _service;

    public IfoodMerchantStatusWatcherBackgroundServiceTests()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IIfoodIntegrationSettingRepository)).Returns(_settingRepository);
        scopedProvider.GetService(typeof(IIfoodMerchantMappingRepository)).Returns(_mappingRepository);
        scopedProvider.GetService(typeof(IBranchRepository)).Returns(_branchRepository);
        scopedProvider.GetService(typeof(IIfoodTokenProvider)).Returns(_tokenProvider);
        scopedProvider.GetService(typeof(IIfoodMerchantClient)).Returns(_merchantClient);
        scopedProvider.GetService(typeof(IIfoodOperationalAlertStore)).Returns(_alertStore);
        scopedProvider.GetService(typeof(IIfoodOpeningHoursRepository)).Returns(_openingHoursRepository);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _service = new IfoodMerchantStatusWatcherBackgroundService(rootProvider, _logger);

        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[1]);
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns("tok");
        _openingHoursRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<IfoodOpeningHours>)[]);
    }

    private Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var method = typeof(IfoodMerchantStatusWatcherBackgroundService).GetMethod("RunCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(_service, [cancellationToken])!;
    }

    private static IfoodMerchantMapping CreateMapping(long branchId, string? merchantId = "MERCH-1")
    {
        var mapping = IfoodMerchantMapping.Create(branchId).Value;
        if (merchantId is not null)
            mapping.SetMerchant(merchantId, "uuid-1");
        return mapping;
    }

    private void SetupOneMapping(long branchId = 10, string merchantId = "MERCH-1")
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [branchId] = CreateMapping(branchId, merchantId) });
    }

    private void SetupStatus(bool success, bool available, string? operationState = null, IReadOnlyCollection<IfoodMerchantValidation>? validations = null)
    {
        _merchantClient.GetStatusAsync("tok", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantStatusResult(success, operationState, available, validations ?? [], success ? null : "erro"));
    }

    [Fact]
    public async Task RunCycleAsync_NoEnabledCompanies_ShouldNotCallMerchantClient()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[]);

        await RunCycleAsync();

        await _merchantClient.DidNotReceiveWithAnyArgs().GetStatusAsync(default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_MappingRepositoryThrows_ShouldLogAndContinue()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("erro"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunCycleAsync_MappingWithoutMerchantId_ShouldSkipBranch()
    {
        SetupOneMapping(merchantId: null!);

        await RunCycleAsync();

        await _merchantClient.DidNotReceiveWithAnyArgs().GetStatusAsync(default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_NoAccessToken_ShouldStopCheckingRemainingBranches()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping>
            {
                [10] = CreateMapping(10, "MERCH-1"),
                [20] = CreateMapping(20, "MERCH-2"),
            });
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns((string?)null);

        await RunCycleAsync();

        await _merchantClient.DidNotReceiveWithAnyArgs().GetStatusAsync(default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_StatusCallFails_ShouldNotAlertOrCrash()
    {
        SetupOneMapping();
        SetupStatus(success: false, available: false);

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_FirstCycle_ShouldOnlyRecordBaselineWithoutAlerting()
    {
        SetupOneMapping();
        SetupStatus(success: true, available: false);

        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_AvailabilityUnchanged_ShouldNotAlert()
    {
        SetupOneMapping();
        SetupStatus(success: true, available: true);

        await RunCycleAsync();
        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToUnavailableWithNoConfiguredShifts_ShouldRaiseCriticalAlert()
    {
        SetupOneMapping();
        var branch = Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value;
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(branch);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false, operationState: "UNAVAILABLE");
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Is<string>(t => t.Contains("indisponível")), Arg.Any<string>(), IfoodOperationalAlertSeverity.Critical);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToUnavailable_ShouldUseValidationMessageAsReasonWhenPresent()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false, operationState: "UNAVAILABLE",
            validations: [new IfoodMerchantValidation("v-1", "FAIL", "excesso de cancelamentos")]);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Is<string>(m => m.Contains("excesso de cancelamentos")), IfoodOperationalAlertSeverity.Critical);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToUnavailableOutsideConfiguredShift_ShouldNotAlert()
    {
        SetupOneMapping();
        // Turno cobrindo um dia da semana diferente do atual, com uma janela curta — nunca cobre "agora".
        var otherDay = ((int)DateTime.Now.DayOfWeek + 3) % 7;
        var shift = IfoodOpeningHours.Create(10, otherDay, new TimeSpan(10, 0, 0), 60).Value;
        _openingHoursRepository.GetByBranchAsync(10, Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<IfoodOpeningHours>)[shift]);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false);
        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionToUnavailableInsideConfiguredShift_ShouldAlert()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);
        // Turno cobrindo o dia atual inteiro (00:00 por 24h) — sempre inclui "agora".
        var shift = IfoodOpeningHours.Create(10, (int)DateTime.Now.DayOfWeek, TimeSpan.Zero, 24 * 60).Value;
        _openingHoursRepository.GetByBranchAsync(10, Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<IfoodOpeningHours>)[shift]);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Any<string>(), IfoodOperationalAlertSeverity.Critical);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionBackToAvailableAfterUnexpectedUnavailability_ShouldRaiseInfoAlert()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false);
        await RunCycleAsync();

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Is<string>(t => t.Contains("voltou a ficar disponível")), Arg.Any<string>(), IfoodOperationalAlertSeverity.Info);
    }

    [Fact]
    public async Task RunCycleAsync_TransitionBackToAvailableAfterExpectedClosure_ShouldNotAlert()
    {
        SetupOneMapping();
        var otherDay = ((int)DateTime.Now.DayOfWeek + 3) % 7;
        var shift = IfoodOpeningHours.Create(10, otherDay, new TimeSpan(10, 0, 0), 60).Value;
        _openingHoursRepository.GetByBranchAsync(10, Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<IfoodOpeningHours>)[shift]);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false); // fechamento esperado (fora do turno) — não alerta
        await RunCycleAsync();

        SetupStatus(success: true, available: true); // reabertura normal — também não deve alertar
        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_BranchNotFound_ShouldUseFallbackBranchName()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        SetupStatus(success: true, available: true);
        await RunCycleAsync();

        SetupStatus(success: true, available: false);
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Filial 10", Arg.Any<string>(), Arg.Any<string>(), IfoodOperationalAlertSeverity.Critical);
    }

    [Fact]
    public async Task RunCycleAsync_CheckBranchThrows_ShouldLogAndNotCrash()
    {
        SetupOneMapping();
        _merchantClient.GetStatusAsync("tok", "MERCH-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ShouldReturnWithoutDoingAnyWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var method = typeof(IfoodMerchantStatusWatcherBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var act = () => (Task)method.Invoke(_service, [cts.Token])!;

        await act.Should().NotThrowAsync();
        await _settingRepository.DidNotReceive().GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>());
    }

    // ---- IsWithinConfiguredShift / turno que cruza a meia-noite ----
    //
    // Os cenários de RunCycleAsync acima só exercitam turnos que cabem no mesmo dia
    // (CoversSameDayShift) — nenhum cobre CoversOvernightShift (ex.: 22h-2h), o próprio caso que a
    // doc do método cita como motivação. Chamado via reflection com "now" explícito (em vez de
    // depender de DateTime.Now real como os testes de RunCycleAsync) para o resultado ser
    // determinístico independente do horário em que a suíte rodar.
    private static bool IsWithinConfiguredShift(IReadOnlyCollection<IfoodOpeningHours> shifts, DateTime now)
    {
        var method = typeof(IfoodMerchantStatusWatcherBackgroundService).GetMethod(
            "IsWithinConfiguredShift", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [shifts, now])!;
    }

    // Segunda-feira 2024-01-01: turno das 22h com 240min de duração cobre até as 02h de terça.
    private static readonly DateTime Monday = new(2024, 1, 1);

    [Fact]
    public void IsWithinConfiguredShift_OvernightShift_MomentSameDayAfterStart_ShouldBeWithin()
    {
        var shift = IfoodOpeningHours.Create(10, (int)Monday.DayOfWeek, new TimeSpan(22, 0, 0), 240).Value;

        var result = IsWithinConfiguredShift([shift], Monday.AddHours(23).AddMinutes(30)); // segunda 23:30

        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinConfiguredShift_OvernightShift_MomentNextDayBeforeEnd_ShouldBeWithin()
    {
        var shift = IfoodOpeningHours.Create(10, (int)Monday.DayOfWeek, new TimeSpan(22, 0, 0), 240).Value;

        var result = IsWithinConfiguredShift([shift], Monday.AddDays(1).AddHours(1).AddMinutes(30)); // terça 01:30

        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinConfiguredShift_OvernightShift_MomentOutsideWindow_ShouldNotBeWithin()
    {
        var shift = IfoodOpeningHours.Create(10, (int)Monday.DayOfWeek, new TimeSpan(22, 0, 0), 240).Value;

        var result = IsWithinConfiguredShift([shift], Monday.AddHours(12)); // segunda 12:00 — bem fora da janela

        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinConfiguredShift_OvernightShift_DifferentDayEntirely_ShouldNotBeWithin()
    {
        var shift = IfoodOpeningHours.Create(10, (int)Monday.DayOfWeek, new TimeSpan(22, 0, 0), 240).Value;

        var result = IsWithinConfiguredShift([shift], Monday.AddDays(2).AddHours(23).AddMinutes(30)); // quarta 23:30

        result.Should().BeFalse();
    }
}
