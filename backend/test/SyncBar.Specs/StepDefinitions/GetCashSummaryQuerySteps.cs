using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetCashSummaryQuerySteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ICashMovementRepository> _cashMovementRepository = new();
    private readonly Mock<IOrderPartialPaymentRepository> _partialPaymentRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Sale> _sales = [];
    private CashSession? _session;
    private Result<CashSummaryResponse>? _result;

    [Given(@"nao existe uma sessao de caixa para resumo com o id (.*)")]
    public void GivenNaoExisteUmaSessaoDeCaixaParaResumoComOId(long cashSessionId)
        => _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"a sessao de caixa (.*) para resumo tem fundo de troco de (.*)")]
    public void GivenASessaoDeCaixaParaResumoTemFundoDeTrocoDe(long cashSessionId, decimal openingAmount)
    {
        _session = CashSession.Open(1, 1, openingAmount).Value;

        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_session);
        _cashMovementRepository
            .Setup(r => r.GetBySessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashMovement>)Array.Empty<CashMovement>());
        _partialPaymentRepository
            .Setup(r => r.GetByCashSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<OrderPartialPayment>)Array.Empty<OrderPartialPayment>());
        _saleRepository
            .Setup(r => r.GetByCashSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Sale>)_sales.AsReadOnly());
    }

    [Given(@"a sessao tem uma venda em dinheiro de (.*)")]
    public void GivenASessaoTemUmaVendaEmDinheiroDe(decimal amount)
    {
        var sale = Sale.Create(1, 1, 1, 1, _sales.Count + 1, amount, 0m, 0m).Value;
        sale.AddPayment(1, amount, null, null, true);
        _sales.Add(sale);
    }

    [Given(@"a sessao tem uma venda no cartao de credito de (.*)")]
    public void GivenASessaoTemUmaVendaNoCartaoDeCreditoDe(decimal amount)
    {
        var sale = Sale.Create(1, 1, 1, 1, _sales.Count + 1, amount, 0m, 0m).Value;
        sale.AddPayment(2, amount, null, null, false);
        _sales.Add(sale);
    }

    [When(@"eu consulto o resumo da sessao de caixa (.*)")]
    public async Task WhenEuConsultoOResumoDaSessaoDeCaixa(long cashSessionId)
    {
        var handler = new GetCashSummaryQueryHandler(
            _cashSessionRepository.Object, _saleRepository.Object, _cashMovementRepository.Object,
            _partialPaymentRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetCashSummaryQuery(cashSessionId), CancellationToken.None);
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

    [Then(@"o total de vendas do resumo deve ser (.*)")]
    public void ThenOTotalDeVendasDoResumoDeveSer(decimal salesTotal)
        => _result!.Value.SalesTotal.Should().Be(salesTotal);

    [Then(@"o resumo deve conter (.*) totais de metodo de pagamento")]
    public void ThenOResumoDeveConterTotaisDeMetodoDePagamento(int count)
        => _result!.Value.PaymentTotals.Should().HaveCount(count);

    [Then(@"o caixa esperado do resumo deve ser (.*)")]
    public void ThenOCaixaEsperadoDoResumoDeveSer(decimal expectedCash)
        => _result!.Value.ExpectedCashAmount.Should().Be(expectedCash);
}
