using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

public sealed class GetIfoodOrderVirtualBagQueryHandlerTests
{
    private const string IfoodOrderExternalId = "Ifood-order-123";
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IIfoodOrderRepository _IfoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodOrderClient _orderClient = Substitute.For<IIfoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private GetIfoodOrderVirtualBagQueryHandler CreateSut() =>
        new(_IfoodOrderRepository, _branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static IfoodOrder CreateIfoodOrder(long branchId = BranchId) =>
        IfoodOrder.Create(
            customerOrderId: 1, branchId: branchId, IfoodOrderId: IfoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", IfoodOrderType: "DELIVERY", deliveredBy: "Ifood", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private void GivenAConnectedBranchWithValidToken(IfoodOrder IfoodOrder)
    {
        _IfoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(IfoodOrder);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
    }

    private static IfoodVirtualBagResult SuccessfulBag() => new(
        Success: true, Id: "bag-1", ShortCode: "ABC123", Status: "PLACED", CreatedAt: DateTime.Now,
        MerchantName: "Bar do Zé", CustomerName: "Maria Silva",
        Items: [new IfoodVirtualBagItemDto("u-1", "Cerveja Long Neck", 2, "7890000000001")],
        GrossValueAmount: "45.00", GrossValueCurrency: "BRL", RawPayload: "{}", ErrorMessage: null);

    [Fact]
    public async Task Handle_WhenIfoodOrderNotFound_ShouldFail()
    {
        _IfoodOrderRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIfoodOrderVirtualBagQuery(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _IfoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIfoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIfoodOrderVirtualBagQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_WhenIfoodTokenUnavailable_ShouldFailWithNotConnected()
    {
        _IfoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIfoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIfoodOrderVirtualBagQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }  
}
