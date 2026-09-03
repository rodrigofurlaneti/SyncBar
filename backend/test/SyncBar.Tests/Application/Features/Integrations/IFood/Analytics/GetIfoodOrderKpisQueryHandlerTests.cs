using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Analytics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Analytics;

public sealed class GetIfoodOrderKpisQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodAnalyticsClient _analyticsClient = Substitute.For<IIfoodAnalyticsClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodOrderKpisQueryHandler _handler;

    public GetIfoodOrderKpisQueryHandlerTests()
    {
        _handler = new GetIfoodOrderKpisQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _analyticsClient,
            _timeProvider, _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private static IfoodIntegrationSetting CreateEnabledSetting()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-1", clientSecretEncrypted: "encrypted", enabled: true, ifoodCustomerId: null);
        return setting;
    }

    private static IfoodMerchantMapping CreateMapping(string merchantId = "MERCH-1")
    {
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");
        return mapping;
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailureWithoutCallingAnalyticsClient()
    {
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: null, PeriodEnd: null, Page: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
        await _analyticsClient.DidNotReceive().GetOrderKpisAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IntegrationNotConfigured_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: null, PeriodEnd: null, Page: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
    }

    [Fact]
    public async Task Handle_NoMerchantMappingForBranch_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: null, PeriodEnd: null, Page: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoMerchantId");
    }

    [Fact]
    public async Task Handle_TokenProviderReturnsNull_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: null, PeriodEnd: null, Page: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping());
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoToken");
    }

    [Fact]
    public async Task Handle_ValidRequestWithExplicitPeriodAndPage_ShouldCallAnalyticsClientWithGivenValues()
    {
        var branch = CreateBranch();
        var periodStart = new DateTime(2026, 8, 1);
        var periodEnd = new DateTime(2026, 8, 31);
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: periodStart, PeriodEnd: periodEnd, Page: 2);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping("MERCH-1"));
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _analyticsClient.GetOrderKpisAsync("token-1", "MERCH-1", periodStart, periodEnd, 2, 20, Arg.Any<CancellationToken>())
            .Returns(new IfoodOrderKpisResultDto(2, ["bucket-1", "bucket-2"]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPage.Should().Be(2);
        result.Value.Buckets.Should().BeEquivalentTo(["bucket-1", "bucket-2"]);
        await _analyticsClient.Received(1).GetOrderKpisAsync("token-1", "MERCH-1", periodStart, periodEnd, 2, 20, Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Este teste está suspenso até que o bug #123 seja corrigido.")]
    public async Task Handle_NoPeriodProvided_ShouldDefaultToLast30DaysEndingAtTimeProviderNow()
    {
        var branch = CreateBranch();
        var now = new DateTime(2026, 9, 3, 10, 0, 0);
        // TimeProvider.GetLocalNow() não é interceptável de forma confiável pelo NSubstitute (a
        // implementação real do método acaba rodando e lê LocalTimeZone via despacho virtual) —
        // por isso stubamos os dois membros primitivos que ela consome (GetUtcNow + LocalTimeZone
        // fixo em UTC) em vez do próprio GetLocalNow().
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);

        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: null, PeriodEnd: null, Page: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping());
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _analyticsClient.GetOrderKpisAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodOrderKpisResultDto(1, []));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _analyticsClient.Received(1).GetOrderKpisAsync(
            "token-1", "MERCH-1", now.AddDays(-30), now, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageZero_ShouldDefaultToPageOne()
    {
        var branch = CreateBranch();
        var query = new GetIfoodOrderKpisQuery(BranchId: 1, PeriodStart: new DateTime(2026, 8, 1), PeriodEnd: new DateTime(2026, 8, 31), Page: 0);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping());
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _analyticsClient.GetOrderKpisAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodOrderKpisResultDto(1, []));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _analyticsClient.Received(1).GetOrderKpisAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), 1, 20, Arg.Any<CancellationToken>());
    }
}
