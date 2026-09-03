using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Logistics;

public sealed class VerifyIfoodDeliveryCodeCommandHandlerTests
{
    private readonly IIfoodOrderRepository _ifoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository = Substitute.For<IIfoodLogisticsDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodLogisticsClient _logisticsClient = Substitute.For<IIfoodLogisticsClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly VerifyIfoodDeliveryCodeCommandHandler _handler;

    public VerifyIfoodDeliveryCodeCommandHandlerTests()
    {
        _handler = new VerifyIfoodDeliveryCodeCommandHandler(
            _ifoodOrderRepository, _deliveryRepository, _branchRepository, _tokenProvider, _logisticsClient,
            _timeProvider, _logRepository, _unitOfWork);

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

    // Pré-requisito de MarkDeliveryCodeVerified: entrega em ArrivedAtDestination.
    private static IfoodLogisticsDelivery CreateDeliveryArrivedAtDestination()
    {
        var delivery = IfoodLogisticsDelivery.Create(1, 1, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value;
        delivery.MarkGoingToOrigin(DateTime.Now);
        delivery.MarkArrivedAtOrigin(DateTime.Now);
        delivery.MarkDispatched(DateTime.Now);
        delivery.MarkArrivedAtDestination(DateTime.Now);
        return delivery;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_DeliveryNotFound_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
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
        var delivery = CreateDeliveryArrivedAtDestination();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
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
        var delivery = CreateDeliveryArrivedAtDestination();
        var branch = CreateBranch();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_IfoodActionFails_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtDestination();
        var branch = CreateBranch();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.VerifyDeliveryCodeAsync("token-1", order.IfoodOrderId, "1234", Arg.Any<CancellationToken>())
            .Returns(new IfoodVerifyDeliveryCodeResult(false, false, "Erro de transporte."));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.ActionFailed");
    }

    [Fact]
    public async Task Handle_CodeNotMatched_ShouldReturnSuccessFalseWithoutCommittingOrTransitioning()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtDestination();
        var branch = CreateBranch();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "0000");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.VerifyDeliveryCodeAsync("token-1", order.IfoodOrderId, "0000", Arg.Any<CancellationToken>())
            .Returns(new IfoodVerifyDeliveryCodeResult(true, false, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        delivery.Status.Should().Be(IfoodLogisticsStatuses.ArrivedAtDestination);
        // Retorna antes do commit explícito — só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CodeMatchedButDeliveryNotArrivedAtDestination_ShouldReturnInvalidTransitionFailure()
    {
        var order = CreateOrder();
        var delivery = IfoodLogisticsDelivery.Create(1, 1, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value; // ainda DriverAssigned
        var branch = CreateBranch();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.VerifyDeliveryCodeAsync("token-1", order.IfoodOrderId, "1234", Arg.Any<CancellationToken>())
            .Returns(new IfoodVerifyDeliveryCodeResult(true, true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
    }

    [Fact]
    public async Task Handle_CodeMatched_ShouldMarkVerifiedAndCommit()
    {
        var order = CreateOrder();
        var delivery = CreateDeliveryArrivedAtDestination();
        var branch = CreateBranch();
        var command = new VerifyIfoodDeliveryCodeCommand(IfoodOrderId: 1, Code: "1234");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.VerifyDeliveryCodeAsync("token-1", order.IfoodOrderId, "1234", Arg.Any<CancellationToken>())
            .Returns(new IfoodVerifyDeliveryCodeResult(true, true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        delivery.Status.Should().Be(IfoodLogisticsStatuses.DeliveryCodeVerified);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
