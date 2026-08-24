using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class AcceptIFoodDisputeCommandHandlerTests
{
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIFoodTokenProvider _tokenProvider = Substitute.For<IIFoodTokenProvider>();
    private readonly IIFoodOrderClient _orderClient = Substitute.For<IIFoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AcceptIFoodDisputeCommandHandler CreateSut() =>
        new(_branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new AcceptIFoodDisputeCommand(BranchId, "dispute-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIFoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new AcceptIFoodDisputeCommand(BranchId, "dispute-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIFoodRejectsAcceptance_ShouldFailWithOriginalMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.AcceptDisputeAsync(ValidToken, "dispute-1", Arg.Any<CancellationToken>())
            .Returns(new IFoodDisputeActionResult(false, null, "Disputa já foi resolvida."));
        var sut = CreateSut();

        var result = await sut.Handle(new AcceptIFoodDisputeCommand(BranchId, "dispute-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.ActionFailed");
        result.Error.Message.Should().Be("Disputa já foi resolvida.");
    }

    [Fact]
    public async Task Handle_WhenIFoodRejectsAcceptanceWithoutMessage_ShouldFailWithDefaultMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.AcceptDisputeAsync(ValidToken, "dispute-1", Arg.Any<CancellationToken>())
            .Returns(new IFoodDisputeActionResult(false, null, null));
        var sut = CreateSut();

        var result = await sut.Handle(new AcceptIFoodDisputeCommand(BranchId, "dispute-1"), CancellationToken.None);

        result.Error.Message.Should().Be("Falha ao aceitar a disputa no iFood.");
    }

    [Fact]
    public async Task Handle_WhenIFoodAcceptsTheDispute_ShouldSucceedWithStatus()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.AcceptDisputeAsync(ValidToken, "dispute-1", Arg.Any<CancellationToken>())
            .Returns(new IFoodDisputeActionResult(true, "ACCEPTED", null));
        var sut = CreateSut();

        var result = await sut.Handle(new AcceptIFoodDisputeCommand(BranchId, "dispute-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Status.Should().Be("ACCEPTED");
    }
}
