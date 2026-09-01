using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.Open;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.Open;

public sealed class OpenOrderCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IComandaSettingRepository _comandaSettingRepository = Substitute.For<IComandaSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly OpenOrderCommandHandler _handler;

    public OpenOrderCommandHandlerTests()
    {
        // TimeProvider.GetLocalNow() não é virtual/mockável — usa TimeProvider.System real
        // (ver convenção do documento de plano); os testes validam estado, não o timestamp exato.
        _handler = new OpenOrderCommandHandler(
            _orderRepository, _diningTableRepository, _comandaRepository, _comandaSettingRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static DiningTable CreateTable(long branchId = 1, long tableStatusId = TableStatusIds.Livre, int number = 1)
        => DiningTable.Create(branchId, tableStatusId, number, 4).Value;

    private static Comanda CreateComanda(long branchId = 1, long comandaStatusId = ComandaStatusIds.Disponivel, string code = "C001")
        => Comanda.Create(branchId, comandaStatusId, code).Value;

    private static ComandaSetting CreateComandaSetting(long branchId, decimal defaultLimitAmount)
        => ComandaSetting.Create(branchId, defaultLimitAmount).Value;

    [Fact]
    public async Task Handle_DiningTableNotFound_ShouldReturnDiningTableNotFound()
    {
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: 10, ComandaId: null, EmployeeId: 1, GuestCount: 2, Notes: null);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiningTableInactive_ShouldReturnDiningTableNotFound()
    {
        var table = CreateTable();
        table.Deactivate();
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: table.Id, ComandaId: null, EmployeeId: 1, GuestCount: 2, Notes: null);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
    }

    [Fact]
    public async Task Handle_DiningTableAlreadyHasOpenOrder_ShouldReturnCustomerOrderTableBusy()
    {
        var table = CreateTable();
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: table.Id, ComandaId: null, EmployeeId: 1, GuestCount: 2, Notes: null);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns(table);
        _orderRepository.HasOpenOrderForTableAsync(table.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.TableBusy");
        table.TableStatusId.Should().Be(TableStatusIds.Livre);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaNotFound_ShouldReturnComandaNotFound()
    {
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: null, ComandaId: 20, EmployeeId: 1, GuestCount: 2, Notes: null);
        _comandaRepository.GetByIdForUpdateAsync(command.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns((Comanda?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaInactive_ShouldReturnComandaNotFound()
    {
        var comanda = CreateComanda();
        comanda.Deactivate();
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: null, ComandaId: comanda.Id, EmployeeId: 1, GuestCount: 2, Notes: null);
        _comandaRepository.GetByIdForUpdateAsync(command.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
    }

    [Fact]
    public async Task Handle_ComandaAlreadyHasOpenOrder_ShouldReturnCustomerOrderComandaBusy()
    {
        var comanda = CreateComanda();
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: null, ComandaId: comanda.Id, EmployeeId: 1, GuestCount: 2, Notes: null);
        _comandaRepository.GetByIdForUpdateAsync(command.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns(comanda);
        _orderRepository.HasOpenOrderForComandaAsync(comanda.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.ComandaBusy");
        comanda.ComandaStatusId.Should().Be(ComandaStatusIds.Disponivel);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpeningOnTableWithoutComanda_ShouldOccupyTableAndCommitTwice()
    {
        var table = CreateTable();
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: table.Id, ComandaId: null, EmployeeId: 7, GuestCount: 3, Notes: "Aniversário");
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns(table);
        _orderRepository.HasOpenOrderForTableAsync(table.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        await _orderRepository.Received(1).AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _comandaSettingRepository.DidNotReceive().GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpeningOnComandaWithConfiguredSetting_ShouldUseSettingDefaultLimitAsCreditLimit()
    {
        var comanda = CreateComanda(branchId: 1);
        var setting = CreateComandaSetting(branchId: 1, defaultLimitAmount: 150m);
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: null, ComandaId: comanda.Id, EmployeeId: 7, GuestCount: 2, Notes: null);
        _comandaRepository.GetByIdForUpdateAsync(command.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns(comanda);
        _orderRepository.HasOpenOrderForComandaAsync(comanda.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _comandaSettingRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(setting);
        CustomerOrder? captured = null;
        await _orderRepository.AddAsync(
            Arg.Do<CustomerOrder>(o => captured = o), Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comanda.ComandaStatusId.Should().Be(ComandaStatusIds.EmUso);
        captured.Should().NotBeNull();
        captured!.CreditLimitAmount.Should().Be(setting.DefaultLimitAmount);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpeningOnComandaWithoutConfiguredSetting_ShouldLeaveCreditLimitNull()
    {
        var comanda = CreateComanda(branchId: 1);
        var command = new OpenOrderCommand(BranchId: 1, DiningTableId: null, ComandaId: comanda.Id, EmployeeId: 7, GuestCount: 2, Notes: null);
        _comandaRepository.GetByIdForUpdateAsync(command.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns(comanda);
        _orderRepository.HasOpenOrderForComandaAsync(comanda.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _comandaSettingRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((ComandaSetting?)null);
        CustomerOrder? captured = null;
        await _orderRepository.AddAsync(
            Arg.Do<CustomerOrder>(o => captured = o), Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.CreditLimitAmount.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeliveryOrderWithoutTableOrComanda_ShouldSucceedWithoutTouchingTableOrComandaRepositories()
    {
        var command = new OpenOrderCommand(
            BranchId: 1, DiningTableId: null, ComandaId: null, EmployeeId: 7, GuestCount: null, Notes: null,
            OrderTypeId: OrderTypeIds.Delivery, CustomerName: "João", CustomerPhone: "11988887777",
            DeliveryAddress: "Rua das Flores, 100");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _comandaRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _orderRepository.Received(1).AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
