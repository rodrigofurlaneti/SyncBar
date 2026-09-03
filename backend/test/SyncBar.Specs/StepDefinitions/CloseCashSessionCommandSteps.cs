using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Features.Cash;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Fechar sessao de caixa")]
public sealed class CloseCashSessionCommandSteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ICashMovementRepository> _cashMovementRepository = new();
    private readonly Mock<IOrderPartialPaymentRepository> _partialPaymentRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<CloseCashSessionResponse>? _result;

    [Given(@"nao existe nenhuma sessao de caixa com o id (.*)")]
    public void GivenNaoExisteNenhumaSessaoDeCaixaComOId(long cashSessionId)
        => _cashSessionRepository
            .Setup(r => r.GetByIdForUpdateAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"a sessao de caixa (.*) ja esta fechada")]
    public void GivenASessaoDeCaixaJaEstaFechada(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);
        SetupSession(cashSessionId, session);
    }

    [Given(@"a sessao de caixa (.*) esta aberta com fundo de troco de (.*)")]
    public void GivenASessaoDeCaixaEstaAbertaComFundoDeTrocoDe(long cashSessionId, decimal openingAmount)
    {
        var session = CashSession.Open(1, 1, openingAmount).Value;
        SetupSession(cashSessionId, session);
    }

    private void SetupSession(long cashSessionId, CashSession session)
    {
        _cashSessionRepository
            .Setup(r => r.GetByIdForUpdateAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _saleRepository
            .Setup(r => r.GetByCashSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Sale>)Array.Empty<Sale>());
        _cashMovementRepository
            .Setup(r => r.GetBySessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashMovement>)Array.Empty<CashMovement>());
        _partialPaymentRepository
            .Setup(r => r.GetByCashSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<OrderPartialPayment>)Array.Empty<OrderPartialPayment>());
    }

    [When(@"eu fecho a sessao de caixa (.*) do funcionario (.*) com valor de fechamento de (.*)")]
    public async Task WhenEuFechoASessaoDeCaixaDoFuncionarioComValorDeFechamentoDe(
        long cashSessionId, long employeeId, decimal closingAmount)
    {
        var handler = new CloseCashSessionCommandHandler(
            _cashSessionRepository.Object, _saleRepository.Object, _cashMovementRepository.Object,
            _partialPaymentRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new CloseCashSessionCommand(cashSessionId, employeeId, closingAmount), CancellationToken.None);
    }

    [Then(@"a operacao deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDeveFalharComOErro(string errorCode)
    {
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a diferenca de caixa apurada deve ser (.*)")]
    public void ThenADiferencaDeCaixaApuradaDeveSer(decimal differenceAmount)
        => _result!.Value.DifferenceAmount.Should().Be(differenceAmount);
}
