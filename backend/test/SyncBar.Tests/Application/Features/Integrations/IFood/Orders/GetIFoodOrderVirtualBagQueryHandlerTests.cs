using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class GetIFoodOrderVirtualBagQueryHandlerTests
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

    private GetIFoodOrderVirtualBagQueryHandler CreateSut() =>
        new(_ifoodOrderRepository, _branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static IFoodOrder CreateIFoodOrder(long branchId = BranchId) =>
        IFoodOrder.Create(
            customerOrderId: 1, branchId: branchId, ifoodOrderId: IFoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", ifoodOrderType: "DELIVERY", deliveredBy: "IFOOD", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private void GivenAConnectedBranchWithValidToken(IFoodOrder ifoodOrder)
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(ifoodOrder);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
    }

    private static IFoodVirtualBagResult SuccessfulBag() => new(
        Success: true, Id: "bag-1", ShortCode: "ABC123", Status: "PLACED", CreatedAt: DateTime.Now,
        MerchantName: "Bar do Zé", CustomerName: "Maria Silva",
        Items: [new IFoodVirtualBagItemDto("u-1", "Cerveja Long Neck", 2, "7890000000001")],
        GrossValueAmount: "45.00", GrossValueCurrency: "BRL", RawPayload: "{}", ErrorMessage: null);

    [Fact]
    public async Task Handle_WhenIFoodOrderNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrderVirtualBagQuery(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrderVirtualBagQuery(1), CancellationToken.None);

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

        var result = await sut.Handle(new GetIFoodOrderVirtualBagQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.NotConnected");
    }

    [Fact]
    public async Task Handle_WhenIFoodFailsToReturnTheBag_ShouldFailWithVirtualBagFailed()
    {
        GivenAConnectedBranchWithValidToken(CreateIFoodOrder());
        _orderClient.GetVirtualBagAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(new IFoodVirtualBagResult(false, null, null, null, null, null, null, [], null, null, null, "Sacola indisponível."));
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrderVirtualBagQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFood.VirtualBagFailed");
        result.Error.Message.Should().Be("Sacola indisponível.");
    }

    [Fact]
    public async Task Handle_WhenIFoodReturnsTheBag_ShouldMapItemsAndTotals()
    {
        GivenAConnectedBranchWithValidToken(CreateIFoodOrder());
        _orderClient.GetVirtualBagAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(SuccessfulBag());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrderVirtualBagQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("bag-1");
        result.Value.CustomerName.Should().Be("Maria Silva");
        result.Value.Items.Should().ContainSingle(i => i.UniqueId == "u-1" && i.Name == "Cerveja Long Neck" && i.Quantity == 2);
        result.Value.GrossValueAmount.Should().Be("45.00");
    }
}
