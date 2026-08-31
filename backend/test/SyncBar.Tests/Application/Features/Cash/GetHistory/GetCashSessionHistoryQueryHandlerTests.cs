using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.GetHistory;

public sealed class GetCashSessionHistoryQueryHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ICashRegisterRepository _cashRegisterRepository = Substitute.For<ICashRegisterRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCashSessionHistoryQueryHandler _handler;

    public GetCashSessionHistoryQueryHandlerTests()
    {
        _handler = new GetCashSessionHistoryQueryHandler(
            _cashSessionRepository, _cashRegisterRepository, _employeeRepository, _saleRepository,
            _logRepository, _unitOfWork);
    }

    private static Sale CreateSale(long cashSessionId, decimal subtotalAmount, long saleNumber = 1001, long customerOrderId = 100)
        => Sale.Create(
            branchId: 1,
            customerOrderId: customerOrderId,
            cashSessionId: cashSessionId,
            employeeId: 5,
            saleNumber: saleNumber,
            subtotalAmount: subtotalAmount,
            discountAmount: 0m,
            serviceFeeAmount: 0m).Value;

    private static Employee CreateEmployee(long branchId, string name)
        => Employee.Create(
            branchId: branchId,
            jobTitleId: 1,
            name: name,
            cpf: "00000000000",
            email: null,
            phone: null,
            hiredAt: DateTime.Now,
            dismissedAt: null,
            salary: null).Value;

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Handle_InvalidReferenceMonth_ShouldReturnInvalidMonthFailure(int month)
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: month);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashHistory.InvalidMonth");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSessionsInPeriod_ShouldReturnEmptyCollectionAndQueryTheCorrectPeriod()
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var expectedFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedTo = expectedFrom.AddMonths(1);

        _cashSessionRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashSession>());
        _cashRegisterRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashRegister>());
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Employee>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        await _cashSessionRepository.Received(1).GetByBranchAndPeriodAsync(
            query.BranchId, expectedFrom, expectedTo, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleSessions_ShouldOrderByOpenedAtDescending()
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var olderSession = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
        // Garante um OpenedAt distinguível do segundo, já que CashSession.Open usa DateTime.Now
        // internamente e a resolução do relógio do Windows pode não diferenciar chamadas muito próximas.
        Thread.Sleep(20);
        var newerSession = CashSession.Open(cashRegisterId: 2, openedByEmployeeId: 20, openingAmount: 200m).Value;

        _cashSessionRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([olderSession, newerSession]);
        _cashRegisterRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashRegister>());
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Employee>());
        _saleRepository.GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        // Id nunca é setado publicamente (sempre 0 nas duas sessões), então diferenciamos as
        // sessões por um campo de negócio (OpeningAmount) em vez de Id.
        result.Value.ElementAt(0).OpeningAmount.Should().Be(newerSession.OpeningAmount);
        result.Value.ElementAt(1).OpeningAmount.Should().Be(olderSession.OpeningAmount);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnmatchedRegisterAndEmployees_ShouldFallBackToDefaultNames()
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var session = CashSession.Open(cashRegisterId: 99, openedByEmployeeId: 55, openingAmount: 300m).Value;
        session.Close(closedByEmployeeId: 77, closingAmount: 310m, expectedAmount: 305m).IsSuccess.Should().BeTrue();

        _cashSessionRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([session]);
        // Listas vazias: nem o registrador nem os funcionários da sessão constam nelas.
        _cashRegisterRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashRegister>());
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Employee>());
        _saleRepository.GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();

        // Mesma interpolação usada pelo handler ($"Caixa {session.CashRegisterId}"), nunca uma
        // string fixa — Id da entidade é sempre 0 neste código-base, então não dá pra simular um
        // CashRegisterId "realista" que também falhe o match; o importante é a interpolação em si.
        response.CashRegisterName.Should().Be($"Caixa {session.CashRegisterId}");
        response.OpenedByName.Should().BeNull();
        response.ClosedByName.Should().BeNull();
        response.CashSessionStatusId.Should().Be(session.CashSessionStatusId);
        response.ClosedAt.Should().Be(session.ClosedAt);
        response.ExpectedAmount.Should().Be(session.ExpectedAmount);
        response.ClosingAmount.Should().Be(session.ClosingAmount);
        response.DifferenceAmount.Should().Be(session.DifferenceAmount);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MatchedRegisterAndEmployee_ShouldResolveNamesInsteadOfFallback()
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        // CashRegisterId/OpenedByEmployeeId/ClosedByEmployeeId = 0 de propósito: como o Id de
        // qualquer entidade agregada é sempre 0 nesta base de código (sem setter público), é a
        // única forma de produzir um match real contra registers/employees simulados — por isso
        // o mesmo funcionário resolve tanto OpenedByName quanto ClosedByName aqui.
        var session = CashSession.Open(cashRegisterId: 0, openedByEmployeeId: 0, openingAmount: 300m).Value;
        session.Close(closedByEmployeeId: 0, closingAmount: 310m, expectedAmount: 305m).IsSuccess.Should().BeTrue();

        var register = CashRegister.Create(branchId: query.BranchId, name: "Caixa Principal").Value;
        var employee = CreateEmployee(query.BranchId, "Ana");

        _cashSessionRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([session]);
        _cashRegisterRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns([register]);
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns([employee]);
        _saleRepository.GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.CashRegisterName.Should().Be(register.Name);
        response.OpenedByName.Should().Be(employee.Name);
        response.ClosedByName.Should().Be(employee.Name);
    }

    [Fact]
    public async Task Handle_ActiveAndInactiveSales_ShouldSumOnlyActiveSalesPerSession()
    {
        var query = new GetCashSessionHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var session = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;

        var activeSale1 = CreateSale(session.Id, subtotalAmount: 50m, saleNumber: 1001);
        var activeSale2 = CreateSale(session.Id, subtotalAmount: 30m, saleNumber: 1002, customerOrderId: 101);
        var inactiveSale = CreateSale(session.Id, subtotalAmount: 999m, saleNumber: 1003, customerOrderId: 102);
        inactiveSale.Deactivate();

        _cashSessionRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([session]);
        _cashRegisterRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashRegister>());
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Employee>());
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns([activeSale1, activeSale2, inactiveSale]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.SalesCount.Should().Be(2);
        response.SalesTotal.Should().Be(activeSale1.TotalAmount + activeSale2.TotalAmount);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
