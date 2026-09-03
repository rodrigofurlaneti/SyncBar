using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Review;

public sealed class ReplyIfoodReviewCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodReviewClient _reviewClient = Substitute.For<IIfoodReviewClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReplyIfoodReviewCommandHandler _handler;

    public ReplyIfoodReviewCommandHandlerTests()
    {
        _handler = new ReplyIfoodReviewCommandHandler(
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
    public async Task Handle_TokenUnavailable_ShouldPropagateResolutionFailureWithoutCallingReviewClient()
    {
        var branch = CreateBranch();
        var command = new ReplyIfoodReviewCommand(BranchId: 1, ReviewId: "rev-1", Text: "Obrigado pelo feedback!");
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant("MERCH-1", "uuid-1");
        _branchRepository.GetByIdAsync(command.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoToken");
        await _reviewClient.DidNotReceive().ReplyReviewAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // Handler não persiste nada localmente (a resposta é gravada só no lado do Ifood) — só o
        // commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldForwardReplyToIfoodAndReturnMappedResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new ReplyIfoodReviewCommand(BranchId: 1, ReviewId: "rev-1", Text: "Obrigado pelo feedback!");
        var createdAt = new DateTime(2026, 9, 3);
        _reviewClient.ReplyReviewAsync("token-1", "MERCH-1", "rev-1", "Obrigado pelo feedback!", Arg.Any<CancellationToken>())
            .Returns(new IfoodReviewReplyResultDto(createdAt, "Obrigado pelo feedback!", "rev-1"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewId.Should().Be("rev-1");
        result.Value.Text.Should().Be("Obrigado pelo feedback!");
        result.Value.CreatedAt.Should().Be(createdAt);
        // Sem estado local pra persistir aqui — mesmo no sucesso, só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
