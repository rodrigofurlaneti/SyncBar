using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Logistics;

public sealed class AssignIfoodDriverCommandHandlerTests
{
    private readonly IIfoodOrderRepository _ifoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository = Substitute.For<IIfoodLogisticsDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodLogisticsClient _logisticsClient = Substitute.For<IIfoodLogisticsClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AssignIfoodDriverCommandHandler _handler;

    public AssignIfoodDriverCommandHandlerTests()
    {
        _handler = new AssignIfoodDriverCommandHandler(
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

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new AssignIfoodDriverCommand(IfoodOrderId: 1, "João", "11999998888", "MOTORCYCLE");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DriverAlreadyAssigned_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var command = new AssignIfoodDriverCommand(IfoodOrderId: 1, "João", "11999998888", "MOTORCYCLE");
        var existingDelivery = IfoodLogisticsDelivery.Create(order.Id, order.BranchId, "Outro", "11900000000", "BIKE", DateTime.Now).Value;
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existingDelivery);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodLogisticsDelivery.AlreadyAssigned");
    }

    [Fact]
    public async Task Handle_TokenUnavailable_ShouldReturnFailureWithoutCallingLogisticsClient()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var command = new AssignIfoodDriverCommand(IfoodOrderId: 1, "João", "11999998888", "MOTORCYCLE");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((IfoodLogisticsDelivery?)null);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
        await _logisticsClient.DidNotReceive().AssignDriverAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IfoodActionFails_ShouldReturnFailureWithoutPersistingDelivery()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var command = new AssignIfoodDriverCommand(IfoodOrderId: 1, "João", "11999998888", "MOTORCYCLE");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((IfoodLogisticsDelivery?)null);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.AssignDriverAsync("token-1", order.IfoodOrderId, "João", "11999998888", "MOTORCYCLE", Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(false, "Pedido não é elegível para frota própria."));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.ActionFailed");
        await _deliveryRepository.DidNotReceive().AddAsync(Arg.Any<IfoodLogisticsDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistDeliveryAndCommit()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var command = new AssignIfoodDriverCommand(IfoodOrderId: 1, "João", "11999998888", "MOTORCYCLE");
        _ifoodOrderRepository.GetByIdForUpdateAsync(command.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _deliveryRepository.GetByIfoodOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((IfoodLogisticsDelivery?)null);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.AssignDriverAsync("token-1", order.IfoodOrderId, "João", "11999998888", "MOTORCYCLE", Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _deliveryRepository.Received(1).AddAsync(
            Arg.Is<IfoodLogisticsDelivery>(d => d.DriverName == "João" && d.IfoodOrderId == order.Id), Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
