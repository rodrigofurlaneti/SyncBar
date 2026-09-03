using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Review;

public sealed class GetIfoodReviewsQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodReviewClient _reviewClient = Substitute.For<IIfoodReviewClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodReviewsQueryHandler _handler;

    public GetIfoodReviewsQueryHandlerTests()
    {
        _handler = new GetIfoodReviewsQueryHandler(
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
    public async Task Handle_IntegrationNotConfigured_ShouldPropagateResolutionFailureWithoutCallingReviewClient()
    {
        var branch = CreateBranch();
        var query = new GetIfoodReviewsQuery(BranchId: 1, Page: 1, PageSize: 20, DateFrom: null, DateTo: null, Sort: "createdAt", SortBy: "DESC");
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
        await _reviewClient.DidNotReceive().GetReviewsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldForwardFiltersAndMapItemsToResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var dateFrom = new DateTime(2026, 8, 1);
        var dateTo = new DateTime(2026, 8, 31);
        var query = new GetIfoodReviewsQuery(BranchId: 1, Page: 2, PageSize: 10, DateFrom: dateFrom, DateTo: dateTo, Sort: "score", SortBy: "ASC");

        var order = new IfoodReviewOrderDto(new DateTime(2026, 8, 15), "order-1", "SHORT1");
        var item = new IfoodReviewListItemDto(
            "rev-1", new DateTime(2026, 8, 16), Discarded: false, Published: true, Comment: "Bom",
            Moderated: false, ModerationStatus: null, Reply: null, Score: 4, SurveyId: null, order);
        _reviewClient.GetReviewsAsync("token-1", "MERCH-1", 2, 10, true, dateFrom, dateTo, "score", "ASC", Arg.Any<CancellationToken>())
            .Returns(new IfoodReviewListResultDto(Page: 2, Size: 10, Total: 1, PageCount: 1, [item]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.Total.Should().Be(1);
        var mapped = result.Value.Reviews.Single();
        mapped.Id.Should().Be("rev-1");
        mapped.Comment.Should().Be("Bom");
        mapped.Order!.ShortId.Should().Be("SHORT1");
        await _reviewClient.Received(1).GetReviewsAsync(
            "token-1", "MERCH-1", 2, 10, true, dateFrom, dateTo, "score", "ASC", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoReviewsReturned_ShouldReturnEmptyItemsWithTotals()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewsQuery(BranchId: 1, Page: 1, PageSize: 20, DateFrom: null, DateTo: null, Sort: "createdAt", SortBy: "DESC");
        _reviewClient.GetReviewsAsync("token-1", "MERCH-1", 1, 20, true, null, null, "createdAt", "DESC", Arg.Any<CancellationToken>())
            .Returns(new IfoodReviewListResultDto(Page: 1, Size: 20, Total: 0, PageCount: 0, []));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reviews.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }
}
