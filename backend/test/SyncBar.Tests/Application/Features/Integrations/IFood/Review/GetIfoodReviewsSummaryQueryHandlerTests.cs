using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Review;

public sealed class GetIfoodReviewsSummaryQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodReviewClient _reviewClient = Substitute.For<IIfoodReviewClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodReviewsSummaryQueryHandler _handler;

    public GetIfoodReviewsSummaryQueryHandlerTests()
    {
        _handler = new GetIfoodReviewsSummaryQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _reviewClient,
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
    public async Task Handle_NoMerchantMapping_ShouldPropagateResolutionFailureWithoutCallingReviewClient()
    {
        var branch = CreateBranch();
        var query = new GetIfoodReviewsSummaryQuery(BranchId: 1);
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoMerchantId");
        await _reviewClient.DidNotReceive().GetSummaryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SummaryFetchFails_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewsSummaryQuery(BranchId: 1);
        _reviewClient.GetSummaryAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns((IfoodReviewSummaryDto?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodReview.SummaryFailed");
    }

    [Fact]
    public async Task Handle_SummaryFetchSucceeds_ShouldMapScoreAndCounts()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewsSummaryQuery(BranchId: 1);
        _reviewClient.GetSummaryAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodReviewSummaryDto(Score: 4.5, TotalReviewsCount: 120, ValidReviewsCount: 100));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(4.5);
        result.Value.TotalReviewsCount.Should().Be(120);
        result.Value.ValidReviewsCount.Should().Be(100);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
