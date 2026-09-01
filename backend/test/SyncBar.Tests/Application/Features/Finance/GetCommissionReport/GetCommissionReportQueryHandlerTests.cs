using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetCommissionReport;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.GetCommissionReport;

// LIMITAÇÃO CONHECIDA: o handler junta Sale/CustomerOrder e agrupa por Employee usando
// igualdade de Id real (s.CustomerOrderId == o.Id, e depois employees.FirstOrDefault(e => e.Id
// == group.Key)). CustomerOrder e Employee só têm construtores privados + fábrica pública
// `.Create(...)`, que sempre inicializa Id como 0 (Id é 'protected set' em Entity, sem setter
// público nem no teste). Por isso não é possível montar de forma confiável, através da API
// pública, um cenário com DUAS CustomerOrder/Employee "diferentes" (Ids distintos) para provar
// o agrupamento por múltiplos funcionários de uma vez — todas as instâncias de teste colidem em
// Id 0. Os testes abaixo cobrem soma/contagem/arredondamento da comissão com uma única
// order/employee (ambos com EmployeeId/Id = 0, o único valor em que a busca por Id funciona de
// forma não ambígua), o fallback de funcionário não encontrado (lista de employees vazia — não
// depende de Id) e a exclusão de vendas sem pedido correspondente no join.
public sealed class GetCommissionReportQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCommissionReportQueryHandler _handler;

    public GetCommissionReportQueryHandlerTests()
    {
        _handler = new GetCommissionReportQueryHandler(
            _saleRepository, _orderRepository, _employeeRepository, _logRepository, _unitOfWork);
    }

    private static Sale CreateSale(long customerOrderId, decimal subtotalAmount, long saleNumber = 1001)
        => Sale.Create(
            branchId: 1, customerOrderId: customerOrderId, cashSessionId: 1, employeeId: 1,
            saleNumber: saleNumber, subtotalAmount: subtotalAmount, discountAmount: 0m, serviceFeeAmount: 0m).Value;

    private static CustomerOrder CreateOrder(long employeeId)
        => CustomerOrder.Create(
            branchId: 1, diningTableId: 10, comandaId: null, employeeId: employeeId,
            guestCount: null, notes: null, Now: DateTime.Now).Value;

    private static Employee CreateEmployee(string name = "Garçom Teste", decimal? commissionPercent = null)
    {
        var employee = Employee.Create(
            branchId: 1, jobTitleId: 1, name: name, cpf: "11122233344", email: null, phone: null,
            hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;
        if (commissionPercent.HasValue)
            employee.SetCommissionPercent(commissionPercent.Value).IsSuccess.Should().BeTrue();
        return employee;
    }

    [Fact]
    public async Task Handle_NoSalesInPeriod_ShouldReturnEmptyCollection()
    {
        var query = new GetCommissionReportQuery(BranchId: 1, From: DateTime.UtcNow.AddDays(-30), To: DateTime.UtcNow);
        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerOrder>());
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TwoSalesForSameOrder_ShouldSumRevenueCountAndRoundCommission()
    {
        var query = new GetCommissionReportQuery(BranchId: 1, From: DateTime.UtcNow.AddDays(-30), To: DateTime.UtcNow);
        var order = CreateOrder(employeeId: 0);
        var sale1 = CreateSale(customerOrderId: order.Id, subtotalAmount: 100m, saleNumber: 1001);
        var sale2 = CreateSale(customerOrderId: order.Id, subtotalAmount: 50m, saleNumber: 1002);
        var employee = CreateEmployee(name: "Garçom Teste", commissionPercent: 10m);

        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns([sale1, sale2]);
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([order]);
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([employee]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var response = result.Value.Single();
        response.EmployeeId.Should().Be(order.EmployeeId);
        response.EmployeeName.Should().Be("Garçom Teste");
        response.CommissionPercent.Should().Be(10m);
        response.SalesCount.Should().Be(2);
        response.Revenue.Should().Be(150m); // 100 + 50
        response.CommissionAmount.Should().Be(15m); // round(150 * 10 / 100, 2)
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeNotFoundInEmployeesList_ShouldUseFallbackNameAndZeroCommission()
    {
        var query = new GetCommissionReportQuery(BranchId: 1, From: DateTime.UtcNow.AddDays(-30), To: DateTime.UtcNow);
        var order = CreateOrder(employeeId: 55);
        var sale = CreateSale(customerOrderId: order.Id, subtotalAmount: 80m);

        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns([sale]);
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([order]);
        // Nenhum funcionário cadastrado para o Id 55 — lista vazia garante ausência de match
        // independentemente da limitação de Id sempre 0 descrita no topo do arquivo.
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.EmployeeId.Should().Be(55);
        response.EmployeeName.Should().Be("Funcionário 55");
        response.CommissionPercent.Should().BeNull();
        response.SalesCount.Should().Be(1);
        response.Revenue.Should().Be(80m);
        response.CommissionAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_SaleWithoutMatchingOrder_ShouldBeExcludedFromResponse()
    {
        var query = new GetCommissionReportQuery(BranchId: 1, From: DateTime.UtcNow.AddDays(-30), To: DateTime.UtcNow);
        var order = CreateOrder(employeeId: 0);
        var matchedSale = CreateSale(customerOrderId: order.Id, subtotalAmount: 40m, saleNumber: 1001);
        // Pedido inexistente na lista retornada por GetByIdsAsync — deve ser silenciosamente
        // excluído pelo INNER JOIN do handler, sem quebrar o cálculo do restante.
        var orphanSale = CreateSale(customerOrderId: 999, subtotalAmount: 999m, saleNumber: 1002);
        var employee = CreateEmployee(commissionPercent: 5m);

        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns([matchedSale, orphanSale]);
        _orderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([order]);
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([employee]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var response = result.Value.Single();
        response.SalesCount.Should().Be(1);
        response.Revenue.Should().Be(40m);
    }
}
