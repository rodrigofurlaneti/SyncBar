using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

public sealed class RequestIfoodDisputeAlternativeCommandHandlerTests
{
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodOrderClient _orderClient = Substitute.For<IIfoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RequestIfoodDisputeAlternativeCommandHandler CreateSut() =>
        new(_branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIfoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIfoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIfoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIfoodRejectsTheAlternative_ShouldFailWithDefaultMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RequestDisputeAlternativeAsync(ValidToken, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL", Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(false, null, null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIfoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("Falha ao propor a alternativa da disputa no Ifood.");
    }

    [Fact]
    public async Task Handle_WhenIfoodAcceptsTheAlternative_ShouldForwardOptionalAmountAndCurrency()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RequestDisputeAlternativeAsync(ValidToken, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL", Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(true, "PENDING", null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIfoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("PENDING");
        await _orderClient.Received(1).RequestDisputeAlternativeAsync(
            ValidToken, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlternativeHasNoValue_ShouldForwardNullAmountAndCurrency()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RequestDisputeAlternativeAsync(ValidToken, "dispute-1", "alt-2", "RESCHEDULE", null, null, Arg.Any<CancellationToken>())
            .Returns(new IfoodDisputeActionResult(true, "PENDING", null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIfoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-2", "RESCHEDULE", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).RequestDisputeAlternativeAsync(
            ValidToken, "dispute-1", "alt-2", "RESCHEDULE", null, null, Arg.Any<CancellationToken>());
    }
}
