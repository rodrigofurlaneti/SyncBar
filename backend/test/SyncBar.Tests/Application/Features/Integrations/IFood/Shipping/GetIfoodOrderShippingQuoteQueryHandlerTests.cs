using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class GetIfoodOrderShippingQuoteQueryHandlerTests
{
    private readonly IIfoodOrderRepository _orderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodOrderShippingQuoteQueryHandler _handler;

    public GetIfoodOrderShippingQuoteQueryHandlerTests()
    {
        _handler = new GetIfoodOrderShippingQuoteQueryHandler(
            _orderRepository, _branchRepository, _tokenProvider, _shippingClient, _logRepository, _unitOfWork);
    }

    private static IfoodOrder CreateOrder() =>
        IfoodOrder.Create(10, 1, "ifood-order-1", null, "merchant-1", "DELIVERY", "MERCHANT", "IMMEDIATE", null, DateTime.UtcNow, false).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var query = new GetIfoodOrderShippingQuoteQuery(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnQuoteFailed()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var query = new GetIfoodOrderShippingQuoteQuery(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.GetDeliveryAvailabilitiesForOrderAsync("token-1", "ifood-order-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(false, "erro remoto", null, 0, 0, 0, 0, 0, 0, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShipping.QuoteFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnMappedQuote()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var query = new GetIfoodOrderShippingQuoteQuery(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.GetDeliveryAvailabilitiesForOrderAsync("token-1", "ifood-order-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(true, null, "quote-1", 10m, 1m, 9m, 20, 40, 1500, DateTime.UtcNow.AddMinutes(10)));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuoteId.Should().Be("quote-1");
        result.Value.NetValue.Should().Be(9m);
    }
}
