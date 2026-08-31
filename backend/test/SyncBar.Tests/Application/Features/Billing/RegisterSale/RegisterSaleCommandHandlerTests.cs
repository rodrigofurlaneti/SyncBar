using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Billing.RegisterSale;

public sealed class RegisterSaleCommandHandlerTests
{
    private static readonly DateTime BaseNow = new(2026, 8, 30, 9, 0, 0);
    private static readonly DateTimeOffset FixedCurrentTime = new(2026, 8, 31, 15, 30, 0, TimeSpan.Zero);

    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private readonly RegisterSaleCommandHandler _handler;

    public RegisterSaleCommandHandlerTests()
    {
        // TimeProvider.GetLocalNow() NÃO é virtual — não dá pra interceptar a chamada em si.
        // A implementação real dela chama GetUtcNow() e LocalTimeZone (esses sim virtuais),
        // então é isso que precisa ser configurado no substituto.
        _timeProvider.GetUtcNow().Returns(FixedCurrentTime);
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);

        // Defaults sensatos — cada teste sobrescreve o que precisar.
        _saleRepository.GetNextSaleNumberAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(777L);
        _saleRepository.ExistsActiveByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _partialPaymentRepository.GetByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        _handler = new RegisterSaleCommandHandler(
            _orderRepository,
            _saleRepository,
            _cashSessionRepository,
            _diningTableRepository,
            _comandaRepository,
            _productRepository,
            _stockItemRepository,
            _stockMovementRepository,
            _partialPaymentRepository,
            _printingService,
            _logRepository,
            _unitOfWork,
            _timeProvider);
    }

    // ---------- Helpers ----------

    private static CustomerOrder CreateOpenOrder(
        long branchId = 1, long? diningTableId = 50, long? comandaId = 60, long employeeId = 5)
    {
        var result = CustomerOrder.Create(
            branchId, diningTableId, comandaId, employeeId, guestCount: 2, notes: null, Now: BaseNow);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // Pedido pronto para pagamento: item ativo cancelado (deve ser ignorado na baixa de estoque)
    // + item de produto controlado + item de produto não controlado.
    private static CustomerOrder CreateAwaitingPaymentOrder(
        long controlledProductId, decimal controlledQuantity,
        long uncontrolledProductId, decimal uncontrolledQuantity,
        long branchId = 1, long? diningTableId = 50, long? comandaId = 60)
    {
        var order = CreateOpenOrder(branchId, diningTableId, comandaId);

        // Item cancelado — sempre o primeiro adicionado (Id=0 em teste, único jeito de mirar nele
        // via UpdateItemStatus é cancelá-lo antes de adicionar os demais).
        order.AddItem(productId: 9999, unitPrice: 999m, quantity: 1, notes: null, employeeId: 5, Now: BaseNow)
            .IsSuccess.Should().BeTrue();
        order.UpdateItemStatus(orderItemId: 0, orderItemStatusId: OrderItemStatusIds.Cancelado, Now: BaseNow, actorEmployeeId: 5)
            .IsSuccess.Should().BeTrue();

        order.AddItem(productId: controlledProductId, unitPrice: 20m, quantity: controlledQuantity, notes: null, employeeId: 5, Now: BaseNow)
            .IsSuccess.Should().BeTrue();
        order.AddItem(productId: uncontrolledProductId, unitPrice: 15m, quantity: uncontrolledQuantity, notes: null, employeeId: 5, Now: BaseNow)
            .IsSuccess.Should().BeTrue();

        order.Close(serviceFeeRate: 0m, Now: BaseNow).IsSuccess.Should().BeTrue();
        return order;
    }

    private static CashSession CreateOpenCashSession(long cashRegisterId = 1)
        => CashSession.Open(cashRegisterId, openedByEmployeeId: 5, openingAmount: 100m).Value;

    private static Product CreateProduct(bool isStockControlled, decimal? costPrice = 8.5m)
        => Product.Create(
            companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: "Produto Teste", description: null,
            barcode: null, salePrice: 20m, costPrice: costPrice, isStockControlled: isStockControlled,
            preparationTimeMinutes: null).Value;

    private static StockItem CreateStockItemWithBalance(decimal currentQuantity, long branchId = 1, long productId = 201)
    {
        var stockItem = StockItem.Create(branchId, productId, minimumQuantity: 0, maximumQuantity: null).Value;
        if (currentQuantity > 0)
            stockItem.Increase(currentQuantity).IsSuccess.Should().BeTrue();
        return stockItem;
    }

    private static RegisterSaleCommand BuildCommand(
        long customerOrderId, long cashSessionId, IReadOnlyCollection<SalePaymentInput> payments, long employeeId = 5)
        => new(customerOrderId, cashSessionId, employeeId, payments);

    // ---------- 1-2. Pedido inválido ----------

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnCustomerOrderNotFound()
    {
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 50m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderInactive_ShouldReturnCustomerOrderNotFound()
    {
        var order = CreateOpenOrder();
        order.Deactivate(BaseNow);
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 50m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotAwaitingPayment_ShouldReturnFailure()
    {
        // Recém-criado: status Aberto, ainda não fechado para pagamento.
        var order = CreateOpenOrder();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 50m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.OrderNotAwaitingPayment");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 3. Sessão de caixa ----------

    [Fact]
    public async Task Handle_CashSessionNotFound_ShouldReturnCashSessionNotOpen()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((CashSession?)null);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionInactive_ShouldReturnCashSessionNotOpen()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        session.Deactivate();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 4. Venda duplicada ----------

    [Fact]
    public async Task Handle_OrderAlreadyHasActiveSale_ShouldReturnSaleDuplicate()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.ExistsActiveByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.Duplicate");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 5. Pagamento inválido ----------

    [Fact]
    public async Task Handle_PaymentWithZeroAmount_ShouldReturnSaleInvalidPaymentAmount()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 0m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.InvalidPaymentAmount");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChangeRequestedOnNonCashPayment_ShouldReturnSaleChangeNotAllowed()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        // Pix não permite troco — ChangeAmount > 0 deve falhar mesmo com amount válido.
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, 10m, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.ChangeNotAllowed");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 6. Pagamento não cobre o total ----------

    [Fact]
    public async Task Handle_PaymentsDoNotCoverTotal_ShouldReturnSaleInsufficientPayment()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m); // total = 90
        var session = CreateOpenCashSession();
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 50m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.InsufficientPayment");

        // Nenhum efeito colateral deve ter ocorrido: nem venda persistida, nem baixa de estoque,
        // nem liberação de mesa/comanda, nem status do pedido alterado.
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _comandaRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // Considerando pagamentos parciais já feitos antes do acerto final (EnsureFullyPaidAsync soma
    // partials + pagamentos da venda). Cobre o ramo em que o total só fecha por causa dos parciais.
    [Fact]
    public async Task Handle_PartialPaymentsCoverTheRemainingBalance_ShouldSucceed()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m); // total = 90
        var session = CreateOpenCashSession();
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var comanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C001").Value;
        var controlledProduct = CreateProduct(isStockControlled: true);
        var uncontrolledProduct = CreateProduct(isStockControlled: false);
        var stockItem = CreateStockItemWithBalance(currentQuantity: 10, productId: 201);

        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _diningTableRepository.GetByIdForUpdateAsync(50, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(60, Arg.Any<CancellationToken>()).Returns(comanda);
        _productRepository.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(controlledProduct);
        _productRepository.GetByIdAsync(202, Arg.Any<CancellationToken>()).Returns(uncontrolledProduct);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 201, Arg.Any<CancellationToken>()).Returns(stockItem);

        var partial = OrderPartialPayment.Create(
            customerOrderId: 100, cashSessionId: 10, paymentMethodId: PaymentMethodIds.Dinheiro, employeeId: 5,
            amount: 30m, authorizationCode: null, payerName: "Cliente que saiu antes").Value;
        // O handler busca os parciais por order.Id (o Id da ENTIDADE, sempre 0 em teste — não há
        // setter público), não pelo CustomerOrderId do comando (100). Configurar com 100 aqui faria
        // a chamada real (order.Id == 0) cair no fallback Arg.Any<long> do construtor (array vazio),
        // fazendo o teste falhar por "Sale.InsufficientPayment" mesmo quando deveria ter sucesso.
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns([partial]);

        // Só 60 pago agora + 30 de parcial = 90, cobre o total exatamente.
        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 60m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _saleRepository.Received(1).AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 8. Fluxo de sucesso completo ----------

    [Fact]
    public async Task Handle_FullSuccessFlow_ShouldPersistSaleReleaseTableAndComandaAndDecreaseStockOnlyForControlledActiveItems()
    {
        var order = CreateAwaitingPaymentOrder(
            controlledProductId: 201, controlledQuantity: 3m,
            uncontrolledProductId: 202, uncontrolledQuantity: 2m); // total = 90
        var session = CreateOpenCashSession();
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var comanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C001").Value;
        var controlledProduct = CreateProduct(isStockControlled: true, costPrice: 8.5m);
        var uncontrolledProduct = CreateProduct(isStockControlled: false);
        var stockItem = CreateStockItemWithBalance(currentQuantity: 10, productId: 201);

        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _diningTableRepository.GetByIdForUpdateAsync(50, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(60, Arg.Any<CancellationToken>()).Returns(comanda);
        _productRepository.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(controlledProduct);
        _productRepository.GetByIdAsync(202, Arg.Any<CancellationToken>()).Returns(uncontrolledProduct);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 201, Arg.Any<CancellationToken>()).Returns(stockItem);
        _printingService.PrintPaymentReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Sale? capturedSale = null;
        _saleRepository.AddAsync(Arg.Do<Sale>(s => capturedSale = s), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var capturedMovements = new List<StockMovement>();
        _stockMovementRepository.AddAsync(Arg.Do<StockMovement>(m => capturedMovements.Add(m)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = BuildCommand(100, 10,
        [
            new SalePaymentInput(PaymentMethodIds.Dinheiro, 40m, 0m, null),
            new SalePaymentInput(PaymentMethodIds.Pix, 50m, null, "AUTH-1"),
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Venda persistida com os dados corretos.
        capturedSale.Should().NotBeNull();
        capturedSale!.BranchId.Should().Be(1);
        capturedSale.CustomerOrderId.Should().Be(order.Id);
        capturedSale.CashSessionId.Should().Be(session.Id);
        capturedSale.SaleNumber.Should().Be(777L);
        capturedSale.TotalAmount.Should().Be(90m);
        capturedSale.Payments.Should().HaveCount(2);
        result.Value.Should().Be(capturedSale.Id);

        // Pedido finalizado.
        order.OrderStatusId.Should().Be(OrderStatusIds.Pago);
        order.ClosedAt.Should().Be(FixedCurrentTime.DateTime);

        // Mesa e comanda liberadas.
        table.TableStatusId.Should().Be(TableStatusIds.Livre);
        comanda.ComandaStatusId.Should().Be(ComandaStatusIds.Disponivel);

        // Baixa de estoque só para o item ativo de produto controlado (item cancelado e item de
        // produto não controlado devem ser ignorados).
        capturedMovements.Should().ContainSingle();
        var movement = capturedMovements.Single();
        movement.StockItemId.Should().Be(stockItem.Id);
        movement.StockMovementTypeId.Should().Be(StockMovementTypeIds.SaidaVenda);
        movement.Quantity.Should().Be(3m);
        movement.UnitCost.Should().Be(8.5m);
        movement.TotalCost.Should().Be(25.5m);
        movement.MovedAt.Should().Be(FixedCurrentTime.DateTime);
        stockItem.CurrentQuantity.Should().Be(7m);

        await _stockItemRepository.DidNotReceive()
            .GetByBranchAndProductForUpdateAsync(1, 202, Arg.Any<CancellationToken>());

        // Recibo impresso e commit duplo (log da BaseCommandHandler + commit explícito de sucesso).
        await _printingService.Received(1).PrintPaymentReceiptAsync(capturedSale.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PrintReceiptThrows_ShouldStillReturnSuccess()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var comanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C001").Value;
        var controlledProduct = CreateProduct(isStockControlled: true);
        var uncontrolledProduct = CreateProduct(isStockControlled: false);
        var stockItem = CreateStockItemWithBalance(currentQuantity: 10, productId: 201);

        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _diningTableRepository.GetByIdForUpdateAsync(50, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(60, Arg.Any<CancellationToken>()).Returns(comanda);
        _productRepository.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(controlledProduct);
        _productRepository.GetByIdAsync(202, Arg.Any<CancellationToken>()).Returns(uncontrolledProduct);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 201, Arg.Any<CancellationToken>()).Returns(stockItem);
        _printingService.PrintPaymentReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Printer offline"));

        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _saleRepository.Received(1).AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- 9. Estoque insuficiente não bloqueia a venda ----------

    [Fact]
    public async Task Handle_InsufficientStockForControlledItem_ShouldStillSucceedWithoutCreatingMovement()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m); // pede 3 unidades do controlado
        var session = CreateOpenCashSession();
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var comanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C001").Value;
        var controlledProduct = CreateProduct(isStockControlled: true);
        var uncontrolledProduct = CreateProduct(isStockControlled: false);
        // Só 1 unidade disponível — Decrease(3) deve falhar (StockItem.InsufficientStock).
        var stockItem = CreateStockItemWithBalance(currentQuantity: 1, productId: 201);

        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _diningTableRepository.GetByIdForUpdateAsync(50, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(60, Arg.Any<CancellationToken>()).Returns(comanda);
        _productRepository.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(controlledProduct);
        _productRepository.GetByIdAsync(202, Arg.Any<CancellationToken>()).Returns(uncontrolledProduct);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 201, Arg.Any<CancellationToken>()).Returns(stockItem);
        _printingService.PrintPaymentReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        stockItem.CurrentQuantity.Should().Be(1m); // saldo não muda quando Decrease falha
        await _saleRepository.Received(1).AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Produto não encontrado / stock item não encontrado: mesmo padrão de "ignora e segue" ----------

    [Fact]
    public async Task Handle_ProductNotFoundForItem_ShouldStillSucceedWithoutCreatingMovement()
    {
        var order = CreateAwaitingPaymentOrder(201, 3m, 202, 2m);
        var session = CreateOpenCashSession();
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var comanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C001").Value;
        // Nenhum produto configurado no repositório: GetByIdAsync retorna null para ambos.
        _orderRepository.GetByIdForUpdateAsync(100, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        _diningTableRepository.GetByIdForUpdateAsync(50, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(60, Arg.Any<CancellationToken>()).Returns(comanda);
        _printingService.PrintPaymentReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = BuildCommand(100, 10, [new SalePaymentInput(PaymentMethodIds.Pix, 90m, null, null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
