using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Review;

public sealed class GetIfoodReviewByIdQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodReviewClient _reviewClient = Substitute.For<IIfoodReviewClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodReviewByIdQueryHandler _handler;

    public GetIfoodReviewByIdQueryHandlerTests()
    {
        _handler = new GetIfoodReviewByIdQueryHandler(
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
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailureWithoutCallingReviewClient()
    {
        var query = new GetIfoodReviewByIdQuery(BranchId: 1, ReviewId: "rev-1");
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
        await _reviewClient.DidNotReceive().GetReviewByIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReviewNotFoundOnIfood_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewByIdQuery(BranchId: 1, ReviewId: "rev-missing");
        _reviewClient.GetReviewByIdAsync("token-1", "MERCH-1", "rev-missing", Arg.Any<CancellationToken>())
            .Returns((IfoodReviewDetailDto?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodReview.NotFound");
    }

    [Fact]
    public async Task Handle_ReviewFound_ShouldMapDetailAndQuestionsToResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewByIdQuery(BranchId: 1, ReviewId: "rev-1");
        var order = new IfoodReviewOrderDto(new DateTime(2026, 9, 1), "order-1", "SHORT1");
        var answers = new[] { new IfoodReviewAnswerOptionDto("a1", "Ótimo") };
        var questions = new[] { new IfoodReviewQuestionDto("q1", "SCALE", "Como foi?", answers) };
        var dto = new IfoodReviewDetailDto(
            "rev-1", new DateTime(2026, 9, 2), Discarded: false, Published: true, Comment: "Muito bom",
            CustomerName: "Fulano", Moderated: false, ModerationStatus: null, Reply: null, Score: 5,
            SurveyId: "s1", order, questions);
        _reviewClient.GetReviewByIdAsync("token-1", "MERCH-1", "rev-1", Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("rev-1");
        result.Value.Comment.Should().Be("Muito bom");
        result.Value.CustomerName.Should().Be("Fulano");
        result.Value.Score.Should().Be(5);
        result.Value.Order.Should().NotBeNull();
        result.Value.Order!.Id.Should().Be("order-1");
        result.Value.Order.ShortId.Should().Be("SHORT1");
        var question = result.Value.Questions.Single();
        question.Id.Should().Be("q1");
        question.Answers.Single().Title.Should().Be("Ótimo");
    }

    [Fact]
    public async Task Handle_ReviewWithoutOrder_ShouldReturnNullOrderInResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReviewByIdQuery(BranchId: 1, ReviewId: "rev-1");
        var dto = new IfoodReviewDetailDto(
            "rev-1", null, Discarded: false, Published: true, Comment: null, CustomerName: null,
            Moderated: false, ModerationStatus: null, Reply: null, Score: null, SurveyId: null,
            Order: null, Questions: []);
        _reviewClient.GetReviewByIdAsync("token-1", "MERCH-1", "rev-1", Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Order.Should().BeNull();
        result.Value.Questions.Should().BeEmpty();
    }
}
