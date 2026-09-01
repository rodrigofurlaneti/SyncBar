using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

public sealed class RejectIfoodDisputeCommandHandlerTests
{
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodOrderClient _orderClient = Substitute.For<IIfoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RejectIfoodDisputeCommandHandler CreateSut() =>
        new(_branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new RejectIfoodDisputeCommand(BranchId, "dispute-1", "Item indisponível"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIfoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new RejectIfoodDisputeCommand(BranchId, "dispute-1", "Item indisponível"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIfoodRejectsTheRejection_ShouldFailWithOriginalMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RejectDisputeAsync(ValidToken, "dispute-1", "Item indisponível", Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(false, null, "Disputa já foi resolvida."));
        var sut = CreateSut();

        var result = await sut.Handle(new RejectIfoodDisputeCommand(BranchId, "dispute-1", "Item indisponível"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.ActionFailed");
        result.Error.Message.Should().Be("Disputa já foi resolvida.");
    }

    [Fact]
    public async Task Handle_WhenIfoodRejectsWithoutMessage_ShouldFailWithDefaultMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RejectDisputeAsync(ValidToken, "dispute-1", "Item indisponível", Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(false, null, null));
        var sut = CreateSut();

        var result = await sut.Handle(new RejectIfoodDisputeCommand(BranchId, "dispute-1", "Item indisponível"), CancellationToken.None);

        result.Error.Message.Should().Be("Falha ao rejeitar a disputa no Ifood.");
    }

    [Fact]
    public async Task Handle_WhenIfoodAcceptsTheRejection_ShouldSucceedWithStatusAndForwardReason()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RejectDisputeAsync(ValidToken, "dispute-1", "Item indisponível", Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(true, "REJECTED", null));
        var sut = CreateSut();

        var result = await sut.Handle(new RejectIfoodDisputeCommand(BranchId, "dispute-1", "Item indisponível"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("REJECTED");
        await _orderClient.Received(1).RejectDisputeAsync(ValidToken, "dispute-1", "Item indisponível", Arg.Any<CancellationToken>());
    }
}
