using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Registrar pagamento parcial de conta de mesa")]
public sealed class RegisterPartialPaymentCommandSteps
{
    private readonly Mock<ICustomerOrderRepository> _orderRepository = new();
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<IOrderPartialPaymentRepository> _partialPaymentRepository = new();
    private readonly Mock<IPrintingService> _printingService = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<OrderPartialPayment> _existingPartials = [];
    private CustomerOrder? _order;
    private Result<long>? _result;

    [Given(@"nao existe nenhum pedido com o id (.*)")]
    public void GivenNaoExisteNenhumPedidoComOId(long orderId)
        => _orderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerOrder?)null);

    [Given(@"um pedido de comanda (.*) sem mesa associada")]
    public void GivenUmPedidoDeComandaSemMesaAssociada(long orderId)
    {
        _order = CustomerOrder.Create(1, null, 99, 1, null, null, DateTime.Now).Value;

        _orderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);
    }

    [Given(@"um pedido de mesa (.*) aberto com total de (.*)")]
    public void GivenUmPedidoDeMesaAbertoComTotalDe(long orderId, decimal total)
    {
        _order = CustomerOrder.Create(1, 5, null, 1, null, null, DateTime.Now).Value;
        _order.AddItem(1, total, 1, null, null, DateTime.Now);

        _orderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);
    }

    [Given(@"um pedido de mesa (.*) ja pago com total de (.*)")]
    public void GivenUmPedidoDeMesaJaPagoComTotalDe(long orderId, decimal total)
    {
        _order = CustomerOrder.Create(1, 5, null, 1, null, null, DateTime.Now).Value;
        _order.AddItem(1, total, 1, null, null, DateTime.Now);
        _order.Close(0m, DateTime.Now);
        _order.MarkAsPaid(DateTime.Now);

        _orderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);
    }

    [Given(@"a sessao de caixa (.*) esta aberta")]
    public void GivenASessaoDeCaixaEstaAberta(long cashSessionId)
    {
        var session = CashSession.Open(1, 1, 0m).Value;
        _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"nao existe uma sessao de caixa aberta com o id (.*)")]
    public void GivenNaoExisteUmaSessaoDeCaixaAbertaComOId(long cashSessionId)
        => _cashSessionRepository
            .Setup(r => r.GetByIdAsync(cashSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashSession?)null);

    [Given(@"o pedido ja tem pagamentos parciais totalizando (.*)")]
    public void GivenOPedidoJaTemPagamentosParciaisTotalizando(decimal amount)
    {
        var partial = OrderPartialPayment.Create(_order!.Id, 1, 1, 1, amount, null, null).Value;
        _existingPartials.Add(partial);
    }

    [When(@"eu registro um pagamento parcial de (.*) no pedido (.*) na sessao de caixa (.*) pelo metodo (.*) do funcionario (.*)")]
    public async Task WhenEuRegistroUmPagamentoParcialNoPedidoNaSessaoDeCaixaPeloMetodoDoFuncionario(
        decimal amount, long orderId, long cashSessionId, long paymentMethodId, long employeeId)
    {
        _partialPaymentRepository
            .Setup(r => r.GetByOrderAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<OrderPartialPayment>)_existingPartials.AsReadOnly());

        var handler = new RegisterPartialPaymentCommandHandler(
            _orderRepository.Object, _cashSessionRepository.Object, _partialPaymentRepository.Object,
            _printingService.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new RegisterPartialPaymentCommand(orderId, cashSessionId, employeeId, paymentMethodId, amount, null, null),
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
