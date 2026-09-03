using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Estornar venda")]
public sealed class RefundSaleCommandSteps
{
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ICustomerOrderRepository> _orderRepository = new();
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ICashMovementRepository> _cashMovementRepository = new();
    private readonly Mock<IDiningTableRepository> _diningTableRepository = new();
    private readonly Mock<IComandaRepository> _comandaRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"nao existe nenhuma venda com o id (.*)")]
    public void GivenNaoExisteNenhumaVendaComOId(long saleId)
        => _saleRepository
            .Setup(r => r.GetByIdForUpdateAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);

    [Given(@"uma venda (.*) ja estornada anteriormente na sessao de caixa (.*)")]
    public void GivenUmaVendaJaEstornadaAnteriormenteNaSessaoDeCaixa(long saleId, long cashSessionId)
    {
        var sale = Sale.Create(1, 10, cashSessionId, 1, saleId, 50m, 0m, 0m).Value;
        sale.Deactivate();

        _saleRepository
            .Setup(r => r.GetByIdForUpdateAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);
    }

    [Given(@"uma venda (.*) ativa na sessao de caixa (.*) no valor de (.*)")]
    public void GivenUmaVendaAtivaNaSessaoDeCaixaNoValorDe(long saleId, long cashSessionId, decimal amount)
    {
        var sale = Sale.Create(1, 10, cashSessionId, 1, saleId, amount, 0m, 0m).Value;

        _saleRepository
            .Setup(r => r.GetByIdForUpdateAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);
    }

    [Given(@"a sessao de caixa (.*) esta aberta")]
    public void GivenASessaoDeCaixaEstaAberta(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"a sessao de caixa (.*) esta fechada")]
    public void GivenASessaoDeCaixaEstaFechada(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 100m).Value;
        session.Close(1, 100m, 100m);

        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [When(@"eu tento estornar a venda (.*) do funcionario (.*)")]
    public async Task WhenEuTentoEstornarAVendaDoFuncionario(long saleId, long employeeId)
    {
        var handler = new RefundSaleCommandHandler(
            _saleRepository.Object, _orderRepository.Object, _cashSessionRepository.Object,
            _cashMovementRepository.Object, _diningTableRepository.Object, _comandaRepository.Object,
            _unitOfWork.Object, TimeProvider.System);

        _result = await handler.Handle(new RefundSaleCommand(saleId, employeeId, null), CancellationToken.None);
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
