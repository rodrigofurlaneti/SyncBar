using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetSalesReport;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.GetSalesReport;

public sealed class GetSalesReportQueryHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 15, 12, 0, 0);

    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetSalesReportQueryHandler _handler;

    public GetSalesReportQueryHandlerTests()
    {
        _handler = new GetSalesReportQueryHandler(
            _saleRepository, _orderRepository, _productRepository, _employeeRepository,
            _logRepository, _unitOfWork);

        // Defaults neutros — cada teste sobrescreve apenas o que precisar.
        _saleRepository.GetByBranchAndPeriodAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerOrder>());
        _orderRepository.GetByBranchAndPeriodAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerOrder>());
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());
        _employeeRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());
    }

    // ---------- Helpers ----------

    // Id só é atribuído pela persistência real (EF Core) — as fábricas do domínio sempre criam
    // com Id=0 (ver `base(0)` nos construtores privados de Product/Employee/CustomerOrder). O
    // handler, porém, faz lookups por entity.Id dentro de coleções retornadas em bloco
    // (GetByIdsAsync) e um Join Sale -> CustomerOrder por `o.Id` — para simular várias entidades
    // distintas nesses cenários sem um DbContext real, atribuímos o Id via reflection. Isso é
    // seguro: `Entity.Id` tem getter público (então `GetProperty` a localiza sem BindingFlags
    // extras) e `SetValue` invoca o setter `protected` sem restrição de acessibilidade em tempo
    // de execução.
    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static CustomerOrder CreateOrder(long id, long branchId, long employeeId)
    {
        var order = CustomerOrder.Create(
            branchId, diningTableId: 1, comandaId: null, employeeId, guestCount: 2, notes: null, Now: FixedNow).Value;
        SetId(order, id);
        return order;
    }

    private static Product CreateProduct(long id, string name, decimal salePrice = 10m)
    {
        var product = Product.Create(
            companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: name, description: null,
            barcode: null, salePrice: salePrice, costPrice: null, isStockControlled: false,
            preparationTimeMinutes: null).Value;
        SetId(product, id);
        return product;
    }

    private static Employee CreateEmployee(long id, string name, string cpf)
    {
        var employee = Employee.Create(
            branchId: 1, jobTitleId: 1, name: name, cpf: cpf, email: null, phone: null,
            hiredAt: FixedNow, dismissedAt: null, salary: null).Value;
        SetId(employee, id);
        return employee;
    }

    private static Sale CreateSale(long customerOrderId, decimal subtotal, decimal serviceFee = 0m, long saleNumber = 1)
        => Sale.Create(
            branchId: 1, customerOrderId: customerOrderId, cashSessionId: 1, employeeId: 1,
            saleNumber: saleNumber, subtotalAmount: subtotal, discountAmount: 0m, serviceFeeAmount: serviceFee).Value;

    // ---------- Tests ----------

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Handle_InvalidReferenceMonth_ReturnsFailureWithoutCallingRepositories(int month)
    {
        var query = new GetSalesReportQuery(1, 2026, month);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SalesReport.InvalidMonth");
        await _saleRepository.DidNotReceive().GetByBranchAndPeriodAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSalesInPeriod_ReturnsZeroedResponseWithoutExceptions()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Revenue.Should().Be(0m);
        response.SalesCount.Should().Be(0);
        response.AverageTicket.Should().Be(0m);
        response.ServiceFeeTotal.Should().Be(0m);
        response.TopProducts.Should().BeEmpty();
        response.SalesByEmployee.Should().BeEmpty();
        response.RevenueByWeekday.Should().HaveCount(7);
        response.RevenueByWeekday.Should().OnlyContain(w => w.Revenue == 0m && w.SalesCount == 0);
        response.RevenueByHour.Should().BeEmpty();
        response.CancelledItemsCount.Should().Be(0);
        response.CancelledItems.Should().BeEmpty();

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SalesFromDifferentEmployees_AggregatesByEmployeeOrderedByRevenueDescending()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var orderA = CreateOrder(id: 101, branchId: 1, employeeId: 10);
        var orderB = CreateOrder(id: 102, branchId: 1, employeeId: 20);

        var saleA = CreateSale(customerOrderId: 101, subtotal: 100m, serviceFee: 10m, saleNumber: 1); // total 110
        var saleB = CreateSale(customerOrderId: 102, subtotal: 300m, serviceFee: 30m, saleNumber: 2); // total 330

        var employeeA = CreateEmployee(10, "Ana", "11111111111");
        var employeeB = CreateEmployee(20, "Bruno", "22222222222");

        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { saleA, saleB });
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { orderA, orderB });
        _employeeRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { employeeA, employeeB });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var byEmployee = result.Value.SalesByEmployee.ToList();
        byEmployee.Should().HaveCount(2);

        // Ordenado por receita desc: Bruno (330) antes de Ana (110).
        byEmployee[0].EmployeeId.Should().Be(20);
        byEmployee[0].EmployeeName.Should().Be("Bruno");
        byEmployee[0].Revenue.Should().Be(330m);
        byEmployee[0].ServiceFee.Should().Be(30m);
        byEmployee[0].SalesCount.Should().Be(1);

        byEmployee[1].EmployeeId.Should().Be(10);
        byEmployee[1].EmployeeName.Should().Be("Ana");
        byEmployee[1].Revenue.Should().Be(110m);
        byEmployee[1].ServiceFee.Should().Be(10m);

        result.Value.Revenue.Should().Be(440m);
        result.Value.SalesCount.Should().Be(2);
        result.Value.AverageTicket.Should().Be(220m);
        result.Value.ServiceFeeTotal.Should().Be(40m);
    }

    [Fact]
    public async Task Handle_EmployeeWithoutMatchInRepository_FallsBackToGenericEmployeeName()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var order = CreateOrder(id: 201, branchId: 1, employeeId: 99);
        var sale = CreateSale(customerOrderId: 201, subtotal: 50m);

        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sale });
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { order });
        // Repositório de funcionários não devolve ninguém com Id=99 (lista vazia dos defaults).

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var byEmployee = result.Value.SalesByEmployee.Single();
        byEmployee.EmployeeId.Should().Be(99);
        byEmployee.EmployeeName.Should().Be("Funcionario 99");
    }

    [Fact]
    public async Task Handle_ItemsFromDifferentProducts_TopProductsAggregatesQuantityAndRevenueExcludingInactiveItems()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var order = CreateOrder(id: 301, branchId: 1, employeeId: 10);
        order.AddItem(productId: 501, unitPrice: 20m, quantity: 3, notes: null, employeeId: 10, Now: FixedNow)
            .IsSuccess.Should().BeTrue(); // 60
        order.AddItem(productId: 502, unitPrice: 15m, quantity: 2, notes: null, employeeId: 10, Now: FixedNow)
            .IsSuccess.Should().BeTrue(); // 30
        order.AddItem(productId: 503, unitPrice: 50m, quantity: 1, notes: null, employeeId: 10, Now: FixedNow)
            .IsSuccess.Should().BeTrue(); // será desativado e deve ser excluído

        order.Items.Last().Deactivate(FixedNow);

        var sale = CreateSale(customerOrderId: 301, subtotal: 90m);

        var productA = CreateProduct(501, "Chopp");
        var productB = CreateProduct(502, "Porção de Batata");
        var productC = CreateProduct(503, "Drink");

        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sale });
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { order });
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { productA, productB, productC });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var topProducts = result.Value.TopProducts.ToList();
        topProducts.Should().HaveCount(2); // produto 503 (item inativo) fica de fora
        topProducts.Select(p => p.ProductId).Should().NotContain(503);

        topProducts[0].ProductId.Should().Be(501);
        topProducts[0].ProductName.Should().Be("Chopp");
        topProducts[0].Quantity.Should().Be(3m);
        topProducts[0].Revenue.Should().Be(60m);

        topProducts[1].ProductId.Should().Be(502);
        topProducts[1].ProductName.Should().Be("Porção de Batata");
        topProducts[1].Quantity.Should().Be(2m);
        topProducts[1].Revenue.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_CancelledItemsInMonth_PopulatesCancelledListWithProductAndCancellingEmployeeNames()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var orderX = CreateOrder(id: 601, branchId: 1, employeeId: 10);
        orderX.AddItem(productId: 701, unitPrice: 25m, quantity: 2, notes: null, employeeId: 10, Now: FixedNow)
            .IsSuccess.Should().BeTrue();
        // Item único do pedido: Id=0 em teste é o único alvo possível de UpdateItemStatus.
        orderX.UpdateItemStatus(orderItemId: 0, orderItemStatusId: OrderItemStatusIds.Cancelado, Now: FixedNow, actorEmployeeId: 30)
            .IsSuccess.Should().BeTrue();

        var orderY = CreateOrder(id: 602, branchId: 1, employeeId: 20);
        orderY.AddItem(productId: 702, unitPrice: 8m, quantity: 5, notes: null, employeeId: 20, Now: FixedNow)
            .IsSuccess.Should().BeTrue();
        orderY.UpdateItemStatus(orderItemId: 0, orderItemStatusId: OrderItemStatusIds.Cancelado, Now: FixedNow, actorEmployeeId: 40)
            .IsSuccess.Should().BeTrue();

        var productX = CreateProduct(701, "Cerveja");
        var productY = CreateProduct(702, "Batata Frita");
        var cancelerA = CreateEmployee(30, "Carla", "33333333333");
        var cancelerB = CreateEmployee(40, "Diego", "44444444444");

        _orderRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { orderX, orderY });
        // GetByIdsAsync de produtos é chamado tanto para os vendidos quanto para os cancelados —
        // como usamos Arg.Any aqui, devolvemos o superset com os dois produtos em ambas as chamadas.
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { productX, productY });
        _employeeRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { cancelerA, cancelerB });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CancelledItemsCount.Should().Be(2);
        var cancelledList = result.Value.CancelledItems.ToList();
        cancelledList.Should().HaveCount(2);
        cancelledList.Should().Contain(c => c.ProductName == "Cerveja" && c.Quantity == 2m && c.CancelledBy == "Carla");
        cancelledList.Should().Contain(c => c.ProductName == "Batata Frita" && c.Quantity == 5m && c.CancelledBy == "Diego");
    }

    [Fact]
    public async Task Handle_ValidPeriodWithSales_WeekdayAndHourBreakdownsHaveExpectedSizesAndDoNotThrow()
    {
        var query = new GetSalesReportQuery(1, 2026, 8);

        var order = CreateOrder(id: 801, branchId: 1, employeeId: 10);
        var sale1 = CreateSale(customerOrderId: 801, subtotal: 50m, saleNumber: 1);
        var sale2 = CreateSale(customerOrderId: 801, subtotal: 70m, saleNumber: 2);

        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sale1, sale2 });
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { order });

        // Não fixamos SoldAt (Sale.Create sempre grava DateTime.Now) nem simulamos um fuso
        // horário determinístico aqui — byWeekday/byHour dependem do fuso local do processo, o
        // que é frágil para travar em CI. Validamos apenas o formato esperado (7 dias da semana,
        // no máximo 24 horas distintas) e que o handler não lança exceção processando o fuso.
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RevenueByWeekday.Should().HaveCount(7);
        result.Value.RevenueByWeekday.Sum(w => w.SalesCount).Should().Be(2);
        result.Value.RevenueByHour.Should().NotBeEmpty();
        result.Value.RevenueByHour.Count.Should().BeLessThanOrEqualTo(24);
    }
}
