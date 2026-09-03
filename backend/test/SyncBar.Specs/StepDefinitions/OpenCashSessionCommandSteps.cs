using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class OpenCashSessionCommandSteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"o caixa (.*) ja tem uma sessao aberta")]
    public void GivenOCaixaJaTemUmaSessaoAberta(long cashRegisterId)
    {
        var session = CashSession.Open(cashRegisterId, 1, 50m).Value;
        _cashSessionRepository
            .Setup(r => r.GetOpenByCashRegisterAsync(cashRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"o caixa (.*) nao tem sessao aberta para abertura")]
    public void GivenOCaixaNaoTemSessaoAbertaParaAbertura(long cashRegisterId)
        => _cashSessionRepository
            .Setup(r => r.GetOpenByCashRegisterAsync(cashRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [When(@"eu abro o caixa (.*) com fundo de troco de (.*) pelo funcionario (.*)")]
    public async Task WhenEuAbroOCaixaComFundoDeTrocoDePeloFuncionario(
        long cashRegisterId, decimal openingAmount, long employeeId)
    {
        var handler = new OpenCashSessionCommandHandler(
            _cashSessionRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new OpenCashSessionCommand(cashRegisterId, employeeId, openingAmount), CancellationToken.None);
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
