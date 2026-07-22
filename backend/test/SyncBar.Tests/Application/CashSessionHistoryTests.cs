using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class CashSessionHistoryTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ICashRegisterRepository _cashRegisterRepository = Substitute.For<ICashRegisterRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task GetHistory_ShouldExposeDifferenceAndSalesTotals()
    {
        // Fechamento com falta de 5: fundo 100, esperado 110, contado 105.
        var session = WithId(CashSession.Open(1, 1, 100m).Value, 9);
        session.Close(2, 105m, 110m);
        _cashSessionRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<CashSession> { session });

        _cashRegisterRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<CashRegister> { WithId(CashRegister.Create(1, "Caixa 01").Value, 1) });

        var opener = WithId(Employee.Create(1, 3, "Ana", "11111111111", null, null, DateTime.UtcNow, null, null).Value, 1);
        var closer = WithId(Employee.Create(1, 1, "Gerente Gil", "22222222222", null, null, DateTime.UtcNow, null, null).Value, 2);
        _employeeRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { opener, closer });

        var sale = Sale.Create(1, 1, 9, 1, 1, 40m, 0m, 0m).Value;
        _saleRepository.GetByCashSessionAsync(9, Arg.Any<CancellationToken>())
            .Returns(new List<Sale> { sale });

        var handler = new GetCashSessionHistoryQueryHandler(
            _cashSessionRepository, _cashRegisterRepository, _employeeRepository, _saleRepository);
        var result = await handler.Handle(new GetCashSessionHistoryQuery(1, 2026, 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Single();
        row.CashRegisterName.Should().Be("Caixa 01");
        row.OpenedByName.Should().Be("Ana");
        row.ClosedByName.Should().Be("Gerente Gil");
        row.DifferenceAmount.Should().Be(-5m);
        row.SalesTotal.Should().Be(40m);
        row.SalesCount.Should().Be(1);
    }

    [Fact]
    public async Task Review_ShouldMarkClosedSessionAsReviewed()
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);
        _cashSessionRepository.GetByIdForUpdateAsync(9, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new ReviewCashSessionCommandHandler(_cashSessionRepository, _unitOfWork);
        var result = await handler.Handle(new ReviewCashSessionCommand(9), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Conferido);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Review_OpenSession_ShouldFail()
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        _cashSessionRepository.GetByIdForUpdateAsync(9, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new ReviewCashSessionCommandHandler(_cashSessionRepository, _unitOfWork);
        var result = await handler.Handle(new ReviewCashSessionCommand(9), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotClosed");
    }
}
