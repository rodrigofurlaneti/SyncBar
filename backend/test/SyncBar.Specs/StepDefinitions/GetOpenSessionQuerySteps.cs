using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Features.Cash;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetOpenSessionQuerySteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<CashSessionResponse>? _result;

    [Given(@"o caixa (.*) nao tem sessao aberta")]
    public void GivenOCaixaNaoTemSessaoAberta(long cashRegisterId)
        => _cashSessionRepository
            .Setup(r => r.GetOpenByCashRegisterAsync(cashRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"o caixa (.*) tem uma sessao aberta com fundo de troco de (.*)")]
    public void GivenOCaixaTemUmaSessaoAbertaComFundoDeTrocoDe(long cashRegisterId, decimal openingAmount)
    {
        var session = CashSession.Open(cashRegisterId, 1, openingAmount).Value;
        _cashSessionRepository
            .Setup(r => r.GetOpenByCashRegisterAsync(cashRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [When(@"eu consulto a sessao aberta do caixa (.*)")]
    public async Task WhenEuConsultoASessaoAbertaDoCaixa(long cashRegisterId)
    {
        var handler = new GetOpenSessionQueryHandler(
            _cashSessionRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetOpenSessionQuery(cashRegisterId), CancellationToken.None);
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

    [Then(@"o fundo de troco da sessao retornada deve ser (.*)")]
    public void ThenOFundoDeTrocoDaSessaoRetornadaDeveSer(decimal openingAmount)
        => _result!.Value.OpeningAmount.Should().Be(openingAmount);
}
