using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.Open;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class OpenOrderCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private OpenOrderCommandHandler CreateHandler()
        => new(_orderRepository, _diningTableRepository, _comandaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithFreeTable_ShouldOpenOrderAndOccupyTable()
    {
        var table = DiningTable.Create(1, TableStatusIds.Livre, 7, 4).Value;
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.HasOpenOrderForTableAsync(10, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(
            new OpenOrderCommand(1, 10, null, 1, 4, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        await _orderRepository.Received(1).AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBusyTable_ShouldFail()
    {
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, 7, 4).Value;
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.HasOpenOrderForTableAsync(10, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new OpenOrderCommand(1, 10, null, 1, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.TableBusy");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownTable_ShouldFailNotFound()
    {
        _diningTableRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await CreateHandler().Handle(
            new OpenOrderCommand(1, 99, null, 1, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
    }
}
