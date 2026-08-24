using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class RequestIFoodDisputeAlternativeCommandHandlerTests
{
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIFoodTokenProvider _tokenProvider = Substitute.For<IIFoodTokenProvider>();
    private readonly IIFoodOrderClient _orderClient = Substitute.For<IIFoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RequestIFoodDisputeAlternativeCommandHandler CreateSut() =>
        new(_branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIFoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIFoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIFoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIFoodRejectsTheAlternative_ShouldFailWithDefaultMessage()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RequestDisputeAlternativeAsync(ValidToken, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL", Arg.Any<CancellationToken>())
            .Returns(new IFoodDisputeActionResult(false, null, null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIFoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("Falha ao propor a alternativa da disputa no iFood.");
    }

    [Fact]
    public async Task Handle_WhenIFoodAcceptsTheAlternative_ShouldForwardOptionalAmountAndCurrency()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.RequestDisputeAlternativeAsync(ValidToken, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL", Arg.Any<CancellationToken>())
            .Returns(new IFoodDisputeActionResult(true, "PENDING", null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIFoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"),
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
            .Returns(new IFoodDisputeActionResult(true, "PENDING", null));
        var sut = CreateSut();

        var result = await sut.Handle(
            new RequestIFoodDisputeAlternativeCommand(BranchId, "dispute-1", "alt-2", "RESCHEDULE", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).RequestDisputeAlternativeAsync(
            ValidToken, "dispute-1", "alt-2", "RESCHEDULE", null, null, Arg.Any<CancellationToken>());
    }
}
