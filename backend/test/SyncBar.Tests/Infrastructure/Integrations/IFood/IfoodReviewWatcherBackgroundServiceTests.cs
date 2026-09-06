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
// 1min + 1h) — RunCycleAsync é invocado via reflection. _lastSeenReviewCreatedAt é estado de
// instância: testes de "avaliação nova" chamam RunCycleAsync mais de uma vez na MESMA instância.
public sealed class IfoodReviewWatcherBackgroundServiceTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodReviewClient _reviewClient = Substitute.For<IIfoodReviewClient>();
    private readonly IIfoodOperationalAlertStore _alertStore = Substitute.For<IIfoodOperationalAlertStore>();
    private readonly ILogger<IfoodReviewWatcherBackgroundService> _logger = NullLogger<IfoodReviewWatcherBackgroundService>.Instance;
    private readonly IfoodReviewWatcherBackgroundService _service;

    public IfoodReviewWatcherBackgroundServiceTests()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IIfoodIntegrationSettingRepository)).Returns(_settingRepository);
        scopedProvider.GetService(typeof(IIfoodMerchantMappingRepository)).Returns(_mappingRepository);
        scopedProvider.GetService(typeof(IBranchRepository)).Returns(_branchRepository);
        scopedProvider.GetService(typeof(IIfoodTokenProvider)).Returns(_tokenProvider);
        scopedProvider.GetService(typeof(IIfoodReviewClient)).Returns(_reviewClient);
        scopedProvider.GetService(typeof(IIfoodOperationalAlertStore)).Returns(_alertStore);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _service = new IfoodReviewWatcherBackgroundService(rootProvider, _logger);

        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[1]);
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns("tok");
    }

    private Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var method = typeof(IfoodReviewWatcherBackgroundService).GetMethod("RunCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(_service, [cancellationToken])!;
    }

    private static IfoodMerchantMapping CreateMapping(long branchId, string? merchantId = "MERCH-1")
    {
        var mapping = IfoodMerchantMapping.Create(branchId).Value;
        if (merchantId is not null)
            mapping.SetMerchant(merchantId, "uuid-1");
        return mapping;
    }

    private void SetupOneMapping(long branchId = 10, string? merchantId = "MERCH-1")
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<long, IfoodMerchantMapping>)new Dictionary<long, IfoodMerchantMapping> { [branchId] = CreateMapping(branchId, merchantId) });
    }

    private static IfoodReviewListItemDto Review(string id, DateTime createdAt, double? score = 5, string? comment = "otimo") =>
        new(id, createdAt, false, true, comment, false, null, null, score, null, null);

    private void SetupReviews(params IfoodReviewListItemDto[] reviews) =>
        _reviewClient.GetReviewsAsync("tok", "MERCH-1", 1, 20, false, null, null, "DESC", "CREATED_AT", Arg.Any<CancellationToken>())
            .Returns(new IfoodReviewListResultDto(1, 20, reviews.Length, 1, reviews));

    [Fact]
    public async Task RunCycleAsync_NoEnabledCompanies_ShouldNotCallReviewClient()
    {
        _settingRepository.GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<long>)[]);

        await RunCycleAsync();

        await _reviewClient.DidNotReceiveWithAnyArgs().GetReviewsAsync(default!, default!, default, default, default, default, default, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_MappingRepositoryThrows_ShouldLogAndContinue()
    {
        _mappingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("erro"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunCycleAsync_NoAccessToken_ShouldSkipCompanyEntirely()
    {
        SetupOneMapping();
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns((string?)null);

        await RunCycleAsync();

        await _reviewClient.DidNotReceiveWithAnyArgs().GetReviewsAsync(default!, default!, default, default, default, default, default, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_MappingWithoutMerchantId_ShouldSkipBranch()
    {
        SetupOneMapping(merchantId: null);

        await RunCycleAsync();

        await _reviewClient.DidNotReceiveWithAnyArgs().GetReviewsAsync(default!, default!, default, default, default, default, default, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_ReviewsWithoutCreatedAt_ShouldNotSetBaselineOrAlert()
    {
        SetupOneMapping();
        SetupReviews(new IfoodReviewListItemDto("r-1", null, false, true, "x", false, null, null, 5, null, null));

        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_FirstCycle_ShouldOnlyRecordBaselineWithoutAlerting()
    {
        SetupOneMapping();
        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));

        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_NoNewReviewsSinceLastCycle_ShouldNotAlertAgain()
    {
        SetupOneMapping();
        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));

        await RunCycleAsync();
        await RunCycleAsync();

        _alertStore.DidNotReceiveWithAnyArgs().Raise(default, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task RunCycleAsync_NewLowScoreReview_ShouldRaiseWarningAlert()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), score: 2, comment: "ruim"));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Is<string>(t => t.Contains("nota baixa")), Arg.Is<string>(m => m.Contains("ruim")), IfoodOperationalAlertSeverity.Warning);
    }

    [Fact]
    public async Task RunCycleAsync_NewHighScoreReview_ShouldRaiseInfoAlert()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), score: 5, comment: "otimo"));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", "Avaliação nova no Ifood", Arg.Any<string>(), IfoodOperationalAlertSeverity.Info);
    }

    [Fact]
    public async Task RunCycleAsync_ScoreExactlyAtThreshold_ShouldBeTreatedAsLowScore()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), score: 3));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Any<string>(), IfoodOperationalAlertSeverity.Warning);
    }

    [Fact]
    public async Task RunCycleAsync_ReviewWithoutScore_ShouldNotBeTreatedAsLowScore()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), score: null));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Is<string>(m => m.Contains("sem nota")), IfoodOperationalAlertSeverity.Info);
    }

    [Fact]
    public async Task RunCycleAsync_ReviewWithoutComment_ShouldUseFallbackText()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), comment: null));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Is<string>(m => m.Contains("sem comentário")), Arg.Any<IfoodOperationalAlertSeverity>());
    }

    [Fact]
    public async Task RunCycleAsync_LongComment_ShouldBeTruncatedWithEllipsis()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Loja Centro", null, null, null, null, null, null, null, null).Value);
        var longComment = new string('a', 200);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2), comment: longComment));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Loja Centro", Arg.Any<string>(), Arg.Is<string>(m => m.Contains("…") && !m.Contains(longComment)), Arg.Any<IfoodOperationalAlertSeverity>());
    }

    [Fact]
    public async Task RunCycleAsync_BranchNotFound_ShouldUseFallbackBranchName()
    {
        SetupOneMapping();
        _branchRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)));
        await RunCycleAsync();

        SetupReviews(Review("r-1", new DateTime(2026, 1, 1)), Review("r-2", new DateTime(2026, 1, 2)));
        await RunCycleAsync();

        _alertStore.Received(1).Raise(1, 10, "Filial 10", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodOperationalAlertSeverity>());
    }

    [Fact]
    public async Task RunCycleAsync_CheckBranchThrows_ShouldLogAndNotCrash()
    {
        SetupOneMapping();
        _reviewClient.GetReviewsAsync("tok", "MERCH-1", 1, 20, false, null, null, "DESC", "CREATED_AT", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var act = () => RunCycleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ShouldReturnWithoutDoingAnyWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var method = typeof(IfoodReviewWatcherBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var act = () => (Task)method.Invoke(_service, [cts.Token])!;

        await act.Should().NotThrowAsync();
        await _settingRepository.DidNotReceive().GetEnabledCompanyIdsAsync(Arg.Any<CancellationToken>());
    }
}
