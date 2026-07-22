using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CustomerOrder : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public long BranchId { get; private set; }
    public long? DiningTableId { get; private set; }
    public long? ComandaId { get; private set; }
    public long EmployeeId { get; private set; }
    public long OrderStatusId { get; private set; }
    // Mesa (padrão) exige DiningTableId/ComandaId; Retirada/Delivery não — usam
    // CustomerName/CustomerPhone/DeliveryAddress no lugar.
    public long OrderTypeId { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? DeliveryAddress { get; private set; }
    // Vínculo opcional com o cadastro de cliente (CRM/fidelidade) — pedidos antigos
    // e pedidos de balcão sem identificação continuam com CustomerId nulo.
    public long? CustomerId { get; private set; }
    public int? GuestCount { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal? CreditLimitAmount { get; private set; }  // limite da comanda (mesa nao tem)
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private CustomerOrder() : base(0) { }

    private CustomerOrder(long branchId, long? diningTableId, long? comandaId, long employeeId, int? guestCount, string? notes, decimal? creditLimitAmount, long orderTypeId, string? customerName, string? customerPhone, string? deliveryAddress, long? customerId) : base(0)
    {
        CreditLimitAmount = comandaId is null ? null : creditLimitAmount;
        BranchId = branchId;
        DiningTableId = diningTableId;
        ComandaId = comandaId;
        EmployeeId = employeeId;
        GuestCount = guestCount;
        Notes = notes;
        OrderTypeId = orderTypeId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        DeliveryAddress = deliveryAddress;
        CustomerId = customerId;
        OrderStatusId = OrderStatusIds.Aberto;
        OpenedAt = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CustomerOrder> Create(
        long branchId, long? diningTableId, long? comandaId, long employeeId, int? guestCount, string? notes,
        decimal? creditLimitAmount = null, long orderTypeId = OrderTypeIds.Mesa,
        string? customerName = null, string? customerPhone = null, string? deliveryAddress = null,
        long? customerId = null)
    {
        // Espelha CK_CustomerOrder_Origin: pedido de MESA precisa de mesa OU comanda.
        // Retirada/Delivery não têm mesa/comanda — usam nome/telefone/endereço do cliente.
        if (orderTypeId == OrderTypeIds.Mesa && diningTableId is null && comandaId is null)
            return Result.Failure<CustomerOrder>(
                new Error("CustomerOrder.MissingOrigin", "Order must have a dining table or a comanda."));

        if (orderTypeId != OrderTypeIds.Mesa && string.IsNullOrWhiteSpace(customerName))
            return Result.Failure<CustomerOrder>(
                new Error("CustomerOrder.MissingCustomerName", "Takeaway/delivery orders require a customer name."));

        if (orderTypeId == OrderTypeIds.Delivery && string.IsNullOrWhiteSpace(deliveryAddress))
            return Result.Failure<CustomerOrder>(
                new Error("CustomerOrder.MissingDeliveryAddress", "Delivery orders require a delivery address."));

        return Result.Success(new CustomerOrder(
            branchId, diningTableId, comandaId, employeeId, guestCount, notes, creditLimitAmount,
            orderTypeId, customerName, customerPhone, deliveryAddress, customerId));
    }

    public Result AddItem(long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId)
    {
        if (!IsOpen())
            return Result.Failure(new Error("CustomerOrder.NotOpen", "Items can only be added to an open order."));
        if (quantity <= 0)
            return Result.Failure(new Error("CustomerOrder.InvalidQuantity", "Quantity must be greater than zero."));

        // Antifraude de comanda perdida: lancamento que ultrapassa o limite e
        // bloqueado — so o gerente libera mais limite.
        if (CreditLimitAmount.HasValue)
        {
            var prospectiveTotal = TotalAmount + Math.Round(unitPrice * quantity, 2);
            if (prospectiveTotal > CreditLimitAmount.Value)
                return Result.Failure(new Error("Comanda.LimitExceeded",
                    $"Limite da comanda atingido (R$ {CreditLimitAmount.Value:N2}, consumo iria a R$ {prospectiveTotal:N2}). Peça ao gerente para liberar mais limite."));
        }

        // UnitPrice congelado no lancamento — nunca recalculado a partir do Product.
        var item = OrderItem.Create(Id, productId, unitPrice, quantity, notes, employeeId);
        if (item.IsFailure)
            return Result.Failure(item.Error);

        _items.Add(item.Value);
        OrderStatusId = OrderStatusIds.EmAndamento;
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateItemStatus(long orderItemId, long orderItemStatusId, long? actorEmployeeId = null)
    {
        if (!IsOpen())
            return Result.Failure(new Error("CustomerOrder.NotOpen", "Order is not open."));

        var item = _items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure(new Error("CustomerOrder.ItemNotFound", "Order item not found."));

        var result = item.UpdateStatus(orderItemStatusId, actorEmployeeId);
        if (result.IsFailure)
            return result;

        if (orderItemStatusId == OrderItemStatusIds.Cancelado)
            RecalculateTotals();

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ApplyDiscount(decimal discountAmount)
    {
        if (!IsOpen())
            return Result.Failure(new Error("CustomerOrder.NotOpen", "Order is not open."));
        if (discountAmount < 0)
            return Result.Failure(new Error("CustomerOrder.InvalidDiscount", "Discount cannot be negative."));
        if (discountAmount > SubtotalAmount)
            return Result.Failure(new Error("CustomerOrder.DiscountExceedsSubtotal", "Discount cannot exceed the subtotal."));

        DiscountAmount = discountAmount;
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Close(decimal serviceFeeRate)
    {
        if (!IsOpen())
            return Result.Failure(new Error("CustomerOrder.NotOpen", "Order is not open."));
        if (_items.Count(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado) == 0)
            return Result.Failure(new Error("CustomerOrder.NoItems", "Order has no items to close."));
        if (serviceFeeRate < 0)
            return Result.Failure(new Error("CustomerOrder.InvalidServiceFee", "Service fee rate cannot be negative."));

        ServiceFeeAmount = Math.Round((SubtotalAmount - DiscountAmount) * serviceFeeRate, 2);
        RecalculateTotals();
        OrderStatusId = OrderStatusIds.AguardandoPagamento;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // So o gerente libera mais limite (validado na API) — e sempre para MAIS.
    public Result RaiseCreditLimit(decimal newLimitAmount)
    {
        if (ComandaId is null)
            return Result.Failure(new Error("Comanda.LimitTableOrder", "Limite de consumo só se aplica a comandas."));
        if (newLimitAmount <= (CreditLimitAmount ?? 0))
            return Result.Failure(new Error("Comanda.LimitMustIncrease",
                $"O novo limite deve ser maior que o atual (R$ {CreditLimitAmount ?? 0:N2})."));

        CreditLimitAmount = newLimitAmount;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Os 10% sao opcionais — a retirada e prerrogativa do gerente (validado na API).
    public Result RemoveServiceFee()
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment",
                "Feche a conta antes de retirar a taxa de serviço."));
        if (ServiceFeeAmount == 0)
            return Result.Failure(new Error("CustomerOrder.NoServiceFee",
                "Esta conta não tem taxa de serviço aplicada."));

        ServiceFeeAmount = 0;
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkAsPaid()
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment", "Order is not awaiting payment."));

        OrderStatusId = OrderStatusIds.Pago;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Estorno da venda ou fechamento por engano: a conta volta a aguardar pagamento/consumo.
    public Result ReopenForPayment()
    {
        if (OrderStatusId != OrderStatusIds.Pago)
            return Result.Failure(new Error("CustomerOrder.NotPaid", "Only a paid order can be reopened by refund."));

        OrderStatusId = OrderStatusIds.AguardandoPagamento;
        ClosedAt = null;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ReopenForConsumption()
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment", "Only a closed (awaiting payment) order can be reopened."));

        OrderStatusId = OrderStatusIds.EmAndamento;
        ServiceFeeAmount = 0;
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (OrderStatusId == OrderStatusIds.Pago)
            return Result.Failure(new Error("CustomerOrder.AlreadyPaid", "Paid orders must be refunded, not cancelled."));
        if (OrderStatusId == OrderStatusIds.Cancelado)
            return Result.Failure(new Error("CustomerOrder.AlreadyCancelled", "Order is already cancelled."));

        OrderStatusId = OrderStatusIds.Cancelado;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private bool IsOpen()
        => OrderStatusId is OrderStatusIds.Aberto or OrderStatusIds.EmAndamento or OrderStatusIds.AguardandoPagamento;

    private void RecalculateTotals()
    {
        SubtotalAmount = _items
            .Where(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado)
            .Sum(i => i.TotalAmount);
        TotalAmount = SubtotalAmount - DiscountAmount + ServiceFeeAmount;
    }
}
