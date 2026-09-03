using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Registrar venda")]
public sealed class RegisterSaleCommandSteps
{
    private readonly Mock<ICustomerOrderRepository> _orderRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<IDiningTableRepository> _diningTableRepository = new();
    private readonly Mock<IComandaRepository> _comandaRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStockItemRepository> _stockItemRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IOrderPartialPaymentRepository> _partialPaymentRepository = new();
    private readonly Mock<IPrintingService> _printingService = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CustomerOrder? _order;
    private Result<long>? _result;

    [Given(@"nao existe nenhum pedido para venda com o id (.*)")]
    public void GivenNaoExisteNenhumPedidoParaVendaComOId(long orderId)
        => _orderRepository
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerOrder?)null);

    [Given(@"um pedido de mesa (.*) ainda em andamento com total de (.*)")]
    public void GivenUmPedidoDeMesaAindaEmAndamentoComTotalDe(long orderId, decimal total)
    {
        _order = CustomerOrder.Create(1, 5, null, 1, null, null, DateTime.Now).Value;
        _order.AddItem(1, total, 1, null, null, DateTime.Now);

        _orderRepository
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);
    }

    [Given(@"um pedido de mesa (.*) aguardando pagamento com total de (.*)")]
    public void GivenUmPedidoDeMesaAguardandoPagamentoComTotalDe(long orderId, decimal total)
    {
        _order = CustomerOrder.Create(1, 5, null, 1, null, null, DateTime.Now).Value;
        _order.AddItem(1, total, 1, null, null, DateTime.Now);
        _order.Close(0m, DateTime.Now);

        _orderRepository
            .Setup(r => r.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);
    }

    [Given(@"a sessao de caixa (.*) esta aberta para vendas")]
    public void GivenASessaoDeCaixaEstaAbertaParaVendas(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 0m).Value;
        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"a sessao de caixa (.*) esta fechada para vendas")]
    public void GivenASessaoDeCaixaEstaFechadaParaVendas(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 0m).Value;
        session.Close(1, 0m, 0m);

        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"o pedido ja possui uma venda ativa registrada")]
    public void GivenOPedidoJaPossuiUmaVendaAtivaRegistrada()
        => _saleRepository
            .Setup(r => r.ExistsActiveByOrderAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    [When(@"eu registro a venda do pedido (.*) na sessao de caixa (.*) do funcionario (.*) com um pagamento de (.*) no metodo (.*)")]
    public async Task WhenEuRegistroAVendaComUmPagamento(
        long orderId, long cashSessionId, long employeeId, decimal amount, long paymentMethodId)
        => await RegisterSaleAsync(orderId, cashSessionId, employeeId,
            [new SalePaymentInput(paymentMethodId, amount, null, null)]);

    [When(@"eu registro a venda do pedido (.*) na sessao de caixa (.*) do funcionario (.*) com um pagamento de (.*) no metodo (.*) e troco de (.*)")]
    public async Task WhenEuRegistroAVendaComUmPagamentoETroco(
        long orderId, long cashSessionId, long employeeId, decimal amount, long paymentMethodId, decimal changeAmount)
        => await RegisterSaleAsync(orderId, cashSessionId, employeeId,
            [new SalePaymentInput(paymentMethodId, amount, changeAmount, null)]);

    private async Task RegisterSaleAsync(
        long orderId, long cashSessionId, long employeeId, IReadOnlyCollection<SalePaymentInput> payments)
    {
        _saleRepository
            .Setup(r => r.GetNextSaleNumberAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RegisterSaleCommandHandler(
            _orderRepository.Object, _saleRepository.Object, _cashSessionRepository.Object,
            _diningTableRepository.Object, _comandaRepository.Object, _productRepository.Object,
            _stockItemRepository.Object, _stockMovementRepository.Object, _partialPaymentRepository.Object,
            _printingService.Object, _logRepository.Object, _unitOfWork.Object, TimeProvider.System);

        _result = await handler.Handle(
            new RegisterSaleCommand(orderId, cashSessionId, employeeId, payments), CancellationToken.None);
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
