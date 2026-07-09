using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class CloseCashSessionCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CloseCashSessionCommandHandler CreateHandler()
        => new(_cashSessionRepository, _saleRepository, _cashMovementRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ShouldComputeExpectedAndDifference()
    {
        // Fundo 100 + venda 50 em dinheiro (troco 10 → líquido 40) − sangria 30 = esperado 110.
        var session = CashSession.Open(1, 1, 100m).Value;
        _cashSessionRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var sale = Sale.Create(1, 1, 1, 1, 1, 40m, 0m, 0m).Value;
        sale.AddPayment(PaymentMethodIds.Dinheiro, 50m, 10m, null, allowsChange: true);
        _saleRepository.GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale> { sale });

        var sangria = CashMovement.Create(1, CashMovementTypeIds.Sangria, null, 1, 30m, null).Value;
        _cashMovementRepository.GetBySessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<CashMovement> { sangria });

        // Contado na gaveta: 105 → falta de 5.
        var result = await CreateHandler().Handle(
            new CloseCashSessionCommand(1, 1, 105m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpectedAmount.Should().Be(110m);
        result.Value.DifferenceAmount.Should().Be(-5m);
        session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Fechado);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosingAlreadyClosedSession_ShouldFail()
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);
        _cashSessionRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale>());
        _cashMovementRepository.GetBySessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<CashMovement>());

        var result = await CreateHandler().Handle(
            new CloseCashSessionCommand(1, 1, 100m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
    }
}
