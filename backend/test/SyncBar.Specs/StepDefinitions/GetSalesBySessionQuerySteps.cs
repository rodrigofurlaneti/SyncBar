using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Billing.GetSalesBySession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar vendas de uma sessao de caixa")]
public sealed class GetSalesBySessionQuerySteps
{
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Sale> _sales = [];
    private Result<IReadOnlyCollection<SessionSaleResponse>>? _result;

    [Given(@"a sessao de caixa (.*) nao tem nenhuma venda")]
    public void GivenASessaoDeCaixaNaoTemNenhumaVenda(long cashSessionId)
        => _saleRepository
            .Setup(r => r.GetByCashSessionAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Sale>)_sales.AsReadOnly());

    [Given(@"a venda (.*) da sessao de caixa (.*) no valor de (.*) com um pagamento no metodo (.*)")]
    public void GivenAVendaDaSessaoDeCaixaNoValorDeComUmPagamentoNoMetodo(
        long saleNumber, long cashSessionId, decimal amount, long paymentMethodId)
    {
        var sale = Sale.Create(1, 100, cashSessionId, 1, saleNumber, amount, 0m, 0m).Value;
        sale.AddPayment(paymentMethodId, amount, null, null, false);
        _sales.Add(sale);

        _saleRepository
            .Setup(r => r.GetByCashSessionAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Sale>)_sales.AsReadOnly());
    }

    [When(@"eu consulto as vendas da sessao de caixa (.*)")]
    public async Task WhenEuConsultoAsVendasDaSessaoDeCaixa(long cashSessionId)
    {
        var handler = new GetSalesBySessionQueryHandler(
            _saleRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetSalesBySessionQuery(cashSessionId), CancellationToken.None);
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

    [Then(@"a lista de vendas retornada deve estar vazia")]
    public void ThenAListaDeVendasRetornadaDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de vendas retornada deve conter (.*) venda")]
    public void ThenAListaDeVendasRetornadaDeveConterVenda(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"o resumo de pagamento da venda deve ser ""(.*)""")]
    public void ThenOResumoDePagamentoDaVendaDeveSer(string summary)
        => _result!.Value.Single().PaymentSummary.Should().ContainSingle(s => s == summary);
}
