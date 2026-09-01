using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.GetOpenByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.GetOpenByBranch;

public sealed class GetOpenOrdersByBranchQueryHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOpenOrdersByBranchQueryHandler _handler;

    public GetOpenOrdersByBranchQueryHandlerTests()
    {
        _handler = new GetOpenOrdersByBranchQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOrder(DateTime now, long branchId = 1, long diningTableId = 1, long employeeId = 1)
        => CustomerOrder.Create(branchId, diningTableId, null, employeeId, 4, null, now).Value;

    [Fact]
    public async Task Handle_NoOpenOrdersForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetOpenOrdersByBranchQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerOrder>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleOpenOrders_ShouldReturnOrderedByOpenedAtAscending()
    {
        var query = new GetOpenOrdersByBranchQuery(BranchId: 1);
        var earlierOrder = CreateOrder(new DateTime(2026, 9, 1, 10, 0, 0));
        var laterOrder = CreateOrder(new DateTime(2026, 9, 1, 12, 0, 0));
        // Retorno do repositório propositalmente fora de ordem para provar o OrderBy do handler.
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([laterOrder, earlierOrder]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.OpenedAt).Should().ContainInOrder(earlierOrder.OpenedAt, laterOrder.OpenedAt);
        // ToResponse() é chamado sem argumento aqui — PartialPaidAmount deve ser 0 por padrão.
        result.Value.Should().OnlyContain(r => r.PartialPaidAmount == 0);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
