using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Logistics;

public sealed class DispatchIfoodLogisticsCommandHandlerTests
{
    private readonly IIfoodOrderRepository _ifoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository = Substitute.For<IIfoodLogisticsDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodLogisticsClient _logisticsClient = Substitute.For<IIfoodLogisticsClient>();
    private readonly IIfoodOrderClient _orderClient = Substitute.For<IIfoodOrderClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DispatchIfoodLogisticsCommandHandler _handler;

    public DispatchIfoodLogisticsCommandHandlerTests()
    {
        _handler = new DispatchIfoodLogisticsCommandHandler(
            _ifoodOrderRepository, _deliveryRepository, _branchRepository, _tokenProvider, _logisticsClient,
            _orderClient, _timeProvider, _logRepository, _unitOfWork);

        var now = new DateTime(2026, 9, 3, 10, 0, 0);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
    }

    private static IfoodOrder CreateOrder()
        => IfoodOrder.Create(
            customerOrderId: 1, branchId: 1, "ifood-order-1", null, "MERCH-1", "DELIVERY", null, "IMMEDIATE", null,
            now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    // Avança a entrega até o status ArrivedAtOrigin (pré-requisito de MarkDispatched).
    private static IfoodLogisticsDelivery CreateDeliveryArrivedAtOrigin()
    {
        var delivery = IfoodLogisticsDelivery.Create(1, 1, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value;
        delivery.MarkGoingToOrigin(DateTime.Now);
        delivery.MarkArrivedAtOrigin(DateTime.Now);
        return delivery;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_DeliveryNotFound_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns((IfoodLogisticsDelivery?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodLogisticsDelivery.NotFound");
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtOrigin();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_TokenUnavailable_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtOrigin();
        var branch = CreateBranch();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_IfoodLogisticsActionFails_ShouldReturnFailureWithoutTransitioning()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtOrigin();
        var branch = CreateBranch();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(false, "Falha ao despachar."));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.ActionFailed");
        delivery.Status.Should().Be(IfoodLogisticsStatuses.ArrivedAtOrigin);
    }

    [Fact]
    public async Task Handle_DeliveryNotArrivedAtOrigin_ShouldReturnInvalidTransitionFailure()
    {
        // Entrega ainda em DriverAssigned — MarkDispatched exige ArrivedAtOrigin.
        var order = CreateOrder();
        var delivery = IfoodLogisticsDelivery.Create(1, 1, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value;
        var branch = CreateBranch();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        // Sem commit explícito quando a transição falha — só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_OrderDispatchSucceeds_ShouldSetOrderStatusAndCommit()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtOrigin();
        var branch = CreateBranch();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(true, null));
        _orderClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodOrderActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(IfoodLogisticsStatuses.Dispatched);
        order.Status.Should().Be(IfoodOrderStatuses.Dispatched);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_OrderDispatchFails_ShouldStillSucceedAndCommit()
    {
        // Best-effort: falha no dispatch do módulo Order não derruba o resultado geral.
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtOrigin();
        var branch = CreateBranch();
        var command = new DispatchIfoodLogisticsCommand(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(true, null));
        _orderClient.DispatchAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodOrderActionResult(false, "Falha no módulo Order."));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(IfoodLogisticsStatuses.Dispatched);
        order.Status.Should().Be(IfoodOrderStatuses.Placed); // não avançou, mas não quebrou o fluxo
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
