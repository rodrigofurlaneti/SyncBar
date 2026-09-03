using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.RegisterMovement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Registrar movimentacao de caixa")]
public sealed class RegisterCashMovementCommandSteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ICashMovementRepository> _cashMovementRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"nao existe uma sessao de caixa para movimentacao com o id (.*)")]
    public void GivenNaoExisteUmaSessaoDeCaixaParaMovimentacaoComOId(long cashSessionId)
        => _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"a sessao de caixa (.*) para movimentacao esta fechada")]
    public void GivenASessaoDeCaixaParaMovimentacaoEstaFechada(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);

        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"a sessao de caixa (.*) para movimentacao esta aberta")]
    public void GivenASessaoDeCaixaParaMovimentacaoEstaAberta(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;

        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [When(@"eu registro uma movimentacao do tipo (.*) no valor de (.*) na sessao de caixa (.*) do funcionario (.*)")]
    public async Task WhenEuRegistroUmaMovimentacaoDoTipoNoValorDeNaSessaoDeCaixaDoFuncionario(
        long movementTypeId, decimal amount, long cashSessionId, long employeeId)
    {
        var handler = new RegisterCashMovementCommandHandler(
            _cashSessionRepository.Object, _cashMovementRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new RegisterCashMovementCommand(cashSessionId, movementTypeId, employeeId, amount, null),
            CancellationToken.None);
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
}
