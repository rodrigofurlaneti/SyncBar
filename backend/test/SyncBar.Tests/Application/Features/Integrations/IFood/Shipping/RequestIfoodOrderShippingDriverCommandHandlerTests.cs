using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class RequestIfoodOrderShippingDriverCommandHandlerTests
{
    private readonly IIfoodOrderRepository _orderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RequestIfoodOrderShippingDriverCommandHandler _handler;

    public RequestIfoodOrderShippingDriverCommandHandlerTests()
    {
        _handler = new RequestIfoodOrderShippingDriverCommandHandler(
            _orderRepository, _branchRepository, _tokenProvider, _shippingClient, _logRepository, _unitOfWork);
    }

    private static IfoodOrder CreateOrder() =>
        IfoodOrder.Create(10, 1, "ifood-order-1", null, "merchant-1", "DELIVERY", null, "IMMEDIATE", null, DateTime.UtcNow, false).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new RequestIfoodOrderShippingDriverCommand(1, "quote-1");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnRequestDriverFailed()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var command = new RequestIfoodOrderShippingDriverCommand(1, "quote-1");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.RequestDriverForOrderAsync("token-1", "ifood-order-1", "quote-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShipping.RequestDriverFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldSucceed()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var command = new RequestIfoodOrderShippingDriverCommand(1, "quote-1");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.RequestDriverForOrderAsync("token-1", "ifood-order-1", "quote-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
