using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Financial;

public sealed class GetIfoodFinancialReportQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodFinancialClient _financialClient = Substitute.For<IIfoodFinancialClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodFinancialReportQueryHandler _handler;

    public GetIfoodFinancialReportQueryHandlerTests()
    {
        _handler = new GetIfoodFinancialReportQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _financialClient,
            _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private void SetupResolvedMerchant(Branch branch, string merchantId = "MERCH-1", string token = "token-1")
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");

        _branchRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(token);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailureWithoutCallingFinancialClient()
    {
        var query = new GetIfoodFinancialReportQuery(BranchId: 1, IfoodFinancialReportType.Payments, null, null, null);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
        await _financialClient.DidNotReceive().GetReportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodFinancialReportType>(), Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnticipationsV3_ShouldCallGetAnticipationsAsync()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodFinancialReportQuery(BranchId: 1, IfoodFinancialReportType.AnticipationsV3, null, null, null);
        _financialClient.GetAnticipationsAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodFinancialReportResultDto(["item-1"]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportType.Should().Be("AnticipationsV3");
        result.Value.Count.Should().Be(1);
        await _financialClient.Received(1).GetAnticipationsAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SalesV3WithExplicitRange_ShouldCallGetSalesV3AsyncWithGivenDates()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var start = new DateTime(2026, 8, 1);
        var end = new DateTime(2026, 8, 31);
        var query = new GetIfoodFinancialReportQuery(BranchId: 1, IfoodFinancialReportType.SalesV3, null, start, end);
        _financialClient.GetSalesV3Async("token-1", "MERCH-1", start, end, 1, Arg.Any<CancellationToken>())
            .Returns(new IfoodFinancialReportResultDto(["item-1", "item-2"]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        await _financialClient.Received(1).GetSalesV3Async("token-1", "MERCH-1", start, end, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SalesV3WithoutRange_ShouldDefaultToLast30DaysEndingToday()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodFinancialReportQuery(BranchId: 1, IfoodFinancialReportType.SalesV3, null, null, null);
        _financialClient.GetSalesV3Async(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodFinancialReportResultDto([]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialClient.Received(1).GetSalesV3Async(
            "token-1", "MERCH-1",
            Arg.Is<DateTime>(d => d.Date == DateTime.Today.AddDays(-30)),
            Arg.Is<DateTime>(d => d.Date == DateTime.Today),
            1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherReportType_ShouldCallGenericGetReportAsyncWithPeriodIdAndRange()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var start = new DateTime(2026, 7, 1);
        var end = new DateTime(2026, 7, 31);
        var query = new GetIfoodFinancialReportQuery(BranchId: 1, IfoodFinancialReportType.Occurrences, "period-1", start, end);
        _financialClient.GetReportAsync("token-1", "MERCH-1", IfoodFinancialReportType.Occurrences, "period-1", start, end, Arg.Any<CancellationToken>())
            .Returns(new IfoodFinancialReportResultDto(["item-1"]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportType.Should().Be("Occurrences");
        await _financialClient.Received(1).GetReportAsync(
            "token-1", "MERCH-1", IfoodFinancialReportType.Occurrences, "period-1", start, end, Arg.Any<CancellationToken>());
    }
}
