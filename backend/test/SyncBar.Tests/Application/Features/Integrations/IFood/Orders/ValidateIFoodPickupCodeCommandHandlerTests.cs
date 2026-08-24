using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class ValidateIFoodPickupCodeCommandHandlerTests
{
    private const string IFoodOrderExternalId = "ifood-order-123";
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IIFoodOrderRepository _ifoodOrderRepository = Substitute.For<IIFoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIFoodTokenProvider _tokenProvider = Substitute.For<IIFoodTokenProvider>();
    private readonly IIFoodOrderClient _orderClient = Substitute.For<IIFoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ValidateIFoodPickupCodeCommandHandler CreateSut() =>
        new(_ifoodOrderRepository, _branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static IFoodOrder CreateIFoodOrder(long branchId = BranchId) =>
        IFoodOrder.Create(
            customerOrderId: 1, branchId: branchId, ifoodOrderId: IFoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", ifoodOrderType: "TAKEOUT", deliveredBy: null, orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private void GivenAConnectedBranchWithValidToken(IFoodOrder ifoodOrder)
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(ifoodOrder);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
    }

    [Fact]
    public async Task Handle_WhenIFoodOrderNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(99, "1234"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(1, "1234"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIFoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(1, "1234"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIFoodRequestItselfFails_ShouldFailWithDefaultMessage()
    {
        GivenAConnectedBranchWithValidToken(CreateIFoodOrder());
        _orderClient.ValidatePickupCodeAsync(ValidToken, IFoodOrderExternalId, "1234", Arg.Any<CancellationToken>())
            .Returns(new IFoodPickupValidationResult(false, false, null));
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(1, "1234"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.ActionFailed");
        result.Error.Message.Should().Be("Falha ao validar o código de retirada no iFood.");
    }

    [Fact]
    public async Task Handle_WhenCodeDoesNotMatch_ShouldSucceedWithFalse()
    {
        GivenAConnectedBranchWithValidToken(CreateIFoodOrder());
        _orderClient.ValidatePickupCodeAsync(ValidToken, IFoodOrderExternalId, "0000", Arg.Any<CancellationToken>())
            .Returns(new IFoodPickupValidationResult(true, false, null));
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(1, "0000"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCodeMatches_ShouldSucceedWithTrue()
    {
        GivenAConnectedBranchWithValidToken(CreateIFoodOrder());
        _orderClient.ValidatePickupCodeAsync(ValidToken, IFoodOrderExternalId, "1234", Arg.Any<CancellationToken>())
            .Returns(new IFoodPickupValidationResult(true, true, null));
        var sut = CreateSut();

        var result = await sut.Handle(new ValidateIFoodPickupCodeCommand(1, "1234"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
