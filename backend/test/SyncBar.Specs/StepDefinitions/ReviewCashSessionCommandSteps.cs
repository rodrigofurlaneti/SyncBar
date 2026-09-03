using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class ReviewCashSessionCommandSteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"nao existe uma sessao de caixa para conferencia com o id (.*)")]
    public void GivenNaoExisteUmaSessaoDeCaixaParaConferenciaComOId(long cashSessionId)
        => _cashSessionRepository
            .Setup(r => r.GetByIdForUpdateAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"a sessao de caixa (.*) para conferencia ainda esta aberta")]
    public void GivenASessaoDeCaixaParaConferenciaAindaEstaAberta(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;

        _cashSessionRepository
            .Setup(r => r.GetByIdForUpdateAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"a sessao de caixa (.*) para conferencia esta fechada")]
    public void GivenASessaoDeCaixaParaConferenciaEstaFechada(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);

        _cashSessionRepository
            .Setup(r => r.GetByIdForUpdateAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [When(@"eu concluo a conferencia da sessao de caixa (.*)")]
    public async Task WhenEuConcluoAConferenciaDaSessaoDeCaixa(long cashSessionId)
    {
        var handler = new ReviewCashSessionCommandHandler(
            _cashSessionRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new ReviewCashSessionCommand(cashSessionId), CancellationToken.None);
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
