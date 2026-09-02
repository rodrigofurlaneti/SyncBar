using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CustomerOrder : AggregateRoot
{
    private const string NotOpenErrorCode = "CustomerOrder.NotOpen";
    private const string OrderNotOpenMessage = "Order is not open.";
    private const string ItemNotFoundErrorCode = "CustomerOrder.ItemNotFound";
    private const string ItemNotFoundMessage = "Order item not found.";

    private readonly List<OrderItem> _items = [];
    public long BranchId { get; private set; }
    public long? DiningTableId { get; private set; }
    public long? ComandaId { get; private set; }
    public long EmployeeId { get; private set; }
    public long OrderStatusId { get; private set; }
    public long OrderTypeId { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public long? CustomerId { get; private set; }
    public int? GuestCount { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal? CreditLimitAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    private CustomerOrder() : base(0) { }
    private CustomerOrder(long branchId, long? diningTableId, long? comandaId, long employeeId, int? guestCount, string? notes, decimal? creditLimitAmount, long orderTypeId, string? customerName, string? customerPhone, string? deliveryAddress, long? customerId, DateTime Now) : base(0)
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
        OpenedAt = Now;
        IsActive = true;
        CreatedAt = Now;
    }

    public static Result<CustomerOrder> Create(
        long branchId, long? diningTableId, long? comandaId, long employeeId, int? guestCount, string? notes,
        DateTime Now,
        decimal? creditLimitAmount = null, long orderTypeId = OrderTypeIds.Mesa,
        string? customerName = null, string? customerPhone = null, string? deliveryAddress = null,
        long? customerId = null)
    {
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
            orderTypeId, customerName, customerPhone, deliveryAddress, customerId, Now));
    }
    public Result ForceCancelItemForTransfer(long orderItemId, DateTime Now, long? actorEmployeeId = null)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, OrderNotOpenMessage));
        var item = _items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure(new Error(ItemNotFoundErrorCode, ItemNotFoundMessage));
        var result = item.ForceCancelForTransfer(actorEmployeeId, Now);
        if (result.IsFailure)
            return result;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result AddItemWithPromotion(Product product, decimal quantity, string? notes, Promotion? activePromotion, long employeeId, DateTime Now)
    {
        var unitPrice = product.SalePrice;
        var finalNotes = notes;
        if (activePromotion?.PromotionTypeId == PromotionTypeIds.Desconto && activePromotion.DiscountRate is not null)
        {
            unitPrice = Math.Round(product.SalePrice * (1 - activePromotion.DiscountRate.Value), 2);
            var tag = $"🏷 {activePromotion.Name} (−{activePromotion.DiscountRate.Value:P0})";
            finalNotes = string.IsNullOrWhiteSpace(finalNotes) ? tag : $"{finalNotes} · {tag}";
        }
        var result = AddItem(product.Id, unitPrice, quantity, finalNotes, employeeId == 0 ? null : employeeId, Now);
        if (result.IsFailure)
            return result;
        if (activePromotion?.PromotionTypeId == PromotionTypeIds.EmDobro)
        {
            var bonus = AddItem(product.Id, 0m, quantity, $"🎁 {activePromotion.Name}", employeeId == 0 ? null : employeeId, Now);
            if (bonus.IsFailure)
                return bonus;
        }
        return Result.Success();
    }

    public Result AddItem(long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, "Items can only be added to an open order."));
        if (quantity <= 0)
            return Result.Failure(new Error("CustomerOrder.InvalidQuantity", "Quantity must be greater than zero."));
        if (CreditLimitAmount.HasValue)
        {
            var prospectiveTotal = TotalAmount + Math.Round(unitPrice * quantity, 2);
            if (prospectiveTotal > CreditLimitAmount.Value)
                return Result.Failure(new Error("Comanda.LimitExceeded",
                    $"Limite da comanda atingido (R$ {CreditLimitAmount.Value:N2}, consumo iria a R$ {prospectiveTotal:N2}). Peça ao gerente para liberar mais limite."));
        }
        long? safeEmployeeId = employeeId.HasValue && employeeId.Value > 0 ? employeeId.Value : null;
        var item = OrderItem.Create(Id, productId, unitPrice, quantity, notes, safeEmployeeId, Now);
        if (item.IsFailure)
            return Result.Failure(item.Error);
        _items.Add(item.Value);
        OrderStatusId = OrderStatusIds.EmAndamento;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    /// <summary>
    /// Adiciona ao pedido um item vindo de uma transferência (mesa↔mesa ou comanda↔comanda),
    /// já nascendo com o status original do item na origem (Lançado, Enviado à Cozinha, Pronto,
    /// Entregue etc.), em vez de sempre nascer "Lançado" e precisar de um UpdateItemStatus posterior.
    /// Existe para evitar reprocurar o item recém-criado por Id logo em seguida: como o Id só é
    /// atribuído pelo EF Core no SaveChanges, todo item novo ainda não salvo tem Id == 0 — ao
    /// transferir vários itens em lote num único commit, mais de um item novo compartilha Id == 0
    /// simultaneamente, e um lookup por Id (FirstOrDefault(i => i.Id == 0)) acaba pegando o primeiro
    /// item com Id 0 da lista, não necessariamente o que acabou de ser adicionado — corrompendo o
    /// status restaurado (ou falhando com "FinalStatus" quando esse primeiro item já está
    /// Entregue/Cancelado). Setar o status direto na criação, por referência, elimina esse problema.
    /// </summary>
    public Result AddTransferredItem(long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, long originalStatusId, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, "Items can only be added to an open order."));
        if (quantity <= 0)
            return Result.Failure(new Error("CustomerOrder.InvalidQuantity", "Quantity must be greater than zero."));
        if (CreditLimitAmount.HasValue)
        {
            var prospectiveTotal = TotalAmount + Math.Round(unitPrice * quantity, 2);
            if (prospectiveTotal > CreditLimitAmount.Value)
                return Result.Failure(new Error("Comanda.LimitExceeded",
                    $"Limite da comanda atingido (R$ {CreditLimitAmount.Value:N2}, consumo iria a R$ {prospectiveTotal:N2}). Peça ao gerente para liberar mais limite."));
        }
        long? safeEmployeeId = employeeId.HasValue && employeeId.Value > 0 ? employeeId.Value : null;
        var item = OrderItem.Create(Id, productId, unitPrice, quantity, notes, safeEmployeeId, Now, originalStatusId);
        if (item.IsFailure)
            return Result.Failure(item.Error);
        _items.Add(item.Value);
        OrderStatusId = OrderStatusIds.EmAndamento;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result AddPizzaItem(
        long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime Now,
        long pizzaSizeId, long? pizzaCrustId, long? pizzaEdgeId, IReadOnlyCollection<long> pizzaFlavorIds)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, "Items can only be added to an open order."));
        if (quantity <= 0)
            return Result.Failure(new Error("CustomerOrder.InvalidQuantity", "Quantity must be greater than zero."));
        if (CreditLimitAmount.HasValue)
        {
            var prospectiveTotal = TotalAmount + Math.Round(unitPrice * quantity, 2);
            if (prospectiveTotal > CreditLimitAmount.Value)
                return Result.Failure(new Error("Comanda.LimitExceeded",
                    $"Limite da comanda atingido (R$ {CreditLimitAmount.Value:N2}, consumo iria a R$ {prospectiveTotal:N2}). Peça ao gerente para liberar mais limite."));
        }
        long? safeEmployeeId = employeeId.HasValue && employeeId.Value > 0 ? employeeId.Value : null;
        var item = OrderItem.CreatePizza(
            Id, productId, unitPrice, quantity, notes, safeEmployeeId, Now,
            pizzaSizeId, pizzaCrustId, pizzaEdgeId, pizzaFlavorIds);
        if (item.IsFailure)
            return Result.Failure(item.Error);
        _items.Add(item.Value);
        OrderStatusId = OrderStatusIds.EmAndamento;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result AddComplement(long orderItemId, long complementId, decimal unitPriceCharged, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, "Items can only be changed on an open order."));
        var item = _items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure(new Error(ItemNotFoundErrorCode, ItemNotFoundMessage));
        var result = item.AddComplement(complementId, unitPriceCharged, Now);
        if (result.IsFailure)
            return result;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }

    public Result RemoveComplement(long orderItemId, long orderItemComplementId, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, "Items can only be changed on an open order."));
        var item = _items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure(new Error(ItemNotFoundErrorCode, ItemNotFoundMessage));
        var result = item.RemoveComplement(orderItemComplementId, Now);
        if (result.IsFailure)
            return result;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result UpdateItemStatus(long orderItemId, long orderItemStatusId, DateTime Now, long? actorEmployeeId = null)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, OrderNotOpenMessage));
        var item = _items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure(new Error(ItemNotFoundErrorCode, ItemNotFoundMessage));
        var result = item.UpdateStatus(orderItemStatusId, actorEmployeeId, Now);
        if (result.IsFailure)
            return result;
        if (orderItemStatusId == OrderItemStatusIds.Cancelado)
            RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result ApplyDiscount(decimal discountAmount, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, OrderNotOpenMessage));
        if (discountAmount < 0)
            return Result.Failure(new Error("CustomerOrder.InvalidDiscount", "Discount cannot be negative."));
        if (discountAmount > SubtotalAmount)
            return Result.Failure(new Error("CustomerOrder.DiscountExceedsSubtotal", "Discount cannot exceed the subtotal."));

        DiscountAmount = discountAmount;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result Close(decimal serviceFeeRate, DateTime Now)
    {
        if (!IsOpen())
            return Result.Failure(new Error(NotOpenErrorCode, OrderNotOpenMessage));
        if (!_items.Any(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado))
            return Result.Failure(new Error("CustomerOrder.NoItems", "Order has no items to close."));
        if (serviceFeeRate < 0)
            return Result.Failure(new Error("CustomerOrder.InvalidServiceFee", "Service fee rate cannot be negative."));

        ServiceFeeAmount = Math.Round((SubtotalAmount - DiscountAmount) * serviceFeeRate, 2);
        RecalculateTotals();
        OrderStatusId = OrderStatusIds.AguardandoPagamento;
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result RaiseCreditLimit(decimal newLimitAmount, DateTime Now)
    {
        if (ComandaId is null)
            return Result.Failure(new Error("Comanda.LimitTableOrder", "Limite de consumo só se aplica a comandas."));
        if (newLimitAmount <= (CreditLimitAmount ?? 0))
            return Result.Failure(new Error("Comanda.LimitMustIncrease",
                $"O novo limite deve ser maior que o atual (R$ {CreditLimitAmount ?? 0:N2})."));

        CreditLimitAmount = newLimitAmount;
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result RemoveServiceFee(DateTime Now)
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment",
                "Feche a conta antes de retirar a taxa de serviço."));
        if (ServiceFeeAmount == 0)
            return Result.Failure(new Error("CustomerOrder.NoServiceFee",
                "Esta conta não tem taxa de serviço aplicada."));

        ServiceFeeAmount = 0;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result MarkAsPaid(DateTime Now)
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment", "Order is not awaiting payment."));

        OrderStatusId = OrderStatusIds.Pago;
        ClosedAt = Now;
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result ReopenForPayment(DateTime Now)
    {
        if (OrderStatusId != OrderStatusIds.Pago)
            return Result.Failure(new Error("CustomerOrder.NotPaid", "Only a paid order can be reopened by refund."));

        OrderStatusId = OrderStatusIds.AguardandoPagamento;
        ClosedAt = null;
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result ReopenForConsumption(DateTime Now)
    {
        if (OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure(new Error("CustomerOrder.NotAwaitingPayment", "Only a closed (awaiting payment) order can be reopened."));

        OrderStatusId = OrderStatusIds.EmAndamento;
        ServiceFeeAmount = 0;
        RecalculateTotals();
        UpdatedAt = Now;
        return Result.Success();
    }
    public Result Cancel(DateTime Now)
    {
        if (OrderStatusId == OrderStatusIds.Pago)
            return Result.Failure(new Error("CustomerOrder.AlreadyPaid", "Paid orders must be refunded, not cancelled."));
        if (OrderStatusId == OrderStatusIds.Cancelado)
            return Result.Failure(new Error("CustomerOrder.AlreadyCancelled", "Order is already cancelled."));

        OrderStatusId = OrderStatusIds.Cancelado;
        ClosedAt = Now;
        UpdatedAt = Now;
        return Result.Success();
    }
    public void Deactivate(DateTime Now)
    {
        IsActive = false;
        UpdatedAt = Now;
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
