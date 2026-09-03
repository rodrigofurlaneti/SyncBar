using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Preparation.GetQueue;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Preparation.GetQueue;

public sealed class GetPreparationQueueQueryHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPreparationQueueQueryHandler _handler;

    public GetPreparationQueueQueryHandlerTests()
    {
        _handler = new GetPreparationQueueQueryHandler(
            _orderRepository, _productRepository, _diningTableRepository, _comandaRepository, _employeeRepository,
            _logRepository, _unitOfWork);

        // Padrão comum a todos os testes: nenhuma mesa/produto por padrão, sobrescrito quando o teste precisa.
        _diningTableRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DiningTable>());
        _employeeRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());
    }

    private static CustomerOrder CreateTableOrder(DateTime now, long diningTableId = 1, long employeeId = 1)
        => CustomerOrder.Create(branchId: 1, diningTableId, comandaId: null, employeeId, guestCount: null, notes: null, now).Value;

    private static CustomerOrder CreateComandaOrder(DateTime now, long comandaId, long employeeId = 1)
        => CustomerOrder.Create(branchId: 1, diningTableId: null, comandaId, employeeId, guestCount: null, notes: null, now).Value;

    [Fact]
    public async Task Handle_NoOpenOrders_ShouldReturnEmptyCollection()
    {
        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerOrder>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithOnlyDeliveredItems_ShouldExcludeOrderFromQueue()
    {
        var now = DateTime.Now;
        var order = CreateTableOrder(now);
        order.AddItem(productId: 100, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, now);
        // Item já entregue: não faz parte de nenhum status "pendente" (Lançado..Pronto).
        order.UpdateItemStatus(orderItemId: 0, OrderItemStatusIds.Entregue, now);

        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([order]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_KitchenItemWithKnownPreparationTime_ShouldMapAsNonBarItemWithProductLimit()
    {
        var now = DateTime.Now;
        var order = CreateTableOrder(now);
        // A fábrica de Product não expõe forma de fixar o Id (nasce sempre 0 em teste — ver
        // convenção documentada em outros handlers). O handler casa item↔produto por Id, então
        // o ProductId lançado no item também precisa ser 0 para "achar" o produto mockado.
        order.AddItem(productId: 0, unitPrice: 25m, quantity: 2, notes: "sem cebola", employeeId: null, now);

        var product = Product.Create(
            companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: "X-Burger", description: null, barcode: null,
            salePrice: 25m, costPrice: null, isStockControlled: false, preparationTimeMinutes: 12).Value;

        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([order]);
        _productRepository.GetByIdsAsync(Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(0)), Arg.Any<CancellationToken>())
            .Returns([product]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value.Single();
        var item = ticket.Items.Single();
        item.ProductName.Should().Be("X-Burger");
        item.IsBarItem.Should().BeFalse();
        item.LimitMinutes.Should().Be(12);
        item.Quantity.Should().Be(2);
        item.Notes.Should().Be("sem cebola");
    }

    [Fact]
    public async Task Handle_ItemWithoutMatchingProduct_ShouldFallBackToBarItemDefaultsAndPlaceholderName()
    {
        var now = DateTime.Now;
        var order = CreateTableOrder(now);
        order.AddItem(productId: 999, unitPrice: 8m, quantity: 1, notes: null, employeeId: null, now);

        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([order]);
        // _productRepository já configurado no ctor para não retornar nenhum produto.

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Single().Items.Single();
        item.ProductName.Should().Be("Produto 999");
        item.IsBarItem.Should().BeTrue();
        item.LimitMinutes.Should().Be(5); // BarToleranceMinutes
    }

    [Fact]
    public async Task Handle_ItemWithoutOwnEmployee_ShouldFallBackToOrderEmployeeForRequestedBy()
    {
        var now = DateTime.Now;
        // Pedido e funcionário nascem com Id 0 (fábrica não persiste) — usar esse Id em
        // ambos é o único jeito de fazer o lookup por Id "casar" nos testes deste handler.
        var order = CreateTableOrder(now, employeeId: 0);
        order.AddItem(productId: 100, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, now);

        var waiter = Employee.Create(
            branchId: 1, jobTitleId: 1, name: "Ana Garçonete", cpf: "111.111.111-11", email: null, phone: null,
            hiredAt: now, dismissedAt: null, salary: null).Value;

        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([order]);
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([waiter]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().Items.Single().RequestedBy.Should().Be("Ana Garçonete");
    }

    [Fact]
    public async Task Handle_ComandaOrder_ShouldResolveComandaCodeViaRepositoryAndLeaveTableNumberNull()
    {
        var now = DateTime.Now;
        var order = CreateComandaOrder(now, comandaId: 42);
        order.AddItem(productId: 100, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, now);

        var comanda = Comanda.Create(branchId: 1, comandaStatusId: 2, code: "C042").Value;

        var query = new GetPreparationQueueQuery(BranchId: 1);
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([order]);
        _comandaRepository.GetByIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(comanda);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value.Single();
        ticket.ComandaCode.Should().Be("C042");
        ticket.TableNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleOrders_ShouldReturnTicketsOrderedByOpenedAtAscending()
    {
        var earlierOrder = CreateTableOrder(new DateTime(2026, 9, 3, 10, 0, 0));
        earlierOrder.AddItem(productId: 100, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, earlierOrder.OpenedAt);

        var laterOrder = CreateTableOrder(new DateTime(2026, 9, 3, 12, 0, 0));
        laterOrder.AddItem(productId: 100, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, laterOrder.OpenedAt);

        var query = new GetPreparationQueueQuery(BranchId: 1);
        // Retorno do repositório propositalmente fora de ordem para provar o OrderBy do handler.
        _orderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([laterOrder, earlierOrder]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(t => t.OpenedAt).Should().ContainInOrder(earlierOrder.OpenedAt, laterOrder.OpenedAt);
    }
}
