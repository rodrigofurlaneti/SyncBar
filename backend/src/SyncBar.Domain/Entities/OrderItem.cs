using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities
{
    public sealed class OrderItem : Entity
    {
        private readonly List<OrderItemComplement> _complements = [];
        private readonly List<OrderItemPizzaFlavor> _pizzaFlavors = [];
        public long CustomerOrderId { get; private set; }
        public long ProductId { get; private set; }
        public long OrderItemStatusId { get; private set; }
        public long? EmployeeId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal DiscountAmount { get; }
        public decimal TotalAmount { get; private set; }
        public string? Notes { get; private set; }
        public DateTime? SentToKitchenAt { get; private set; }
        public DateTime? DeliveredAt { get; private set; }
        public long? CancelledByEmployeeId { get; private set; }
        public long? PizzaSizeId { get; private set; }
        public long? PizzaCrustId { get; private set; }
        public long? PizzaEdgeId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }
        public IReadOnlyCollection<OrderItemComplement> Complements => _complements.AsReadOnly();
        public IReadOnlyCollection<OrderItemPizzaFlavor> PizzaFlavors => _pizzaFlavors.AsReadOnly();
        private OrderItem() : base(0) { }
        private OrderItem(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime Now,
            long? pizzaSizeId = null, long? pizzaCrustId = null, long? pizzaEdgeId = null) : base(0)
        {
            CustomerOrderId = customerOrderId;
            ProductId = productId;
            UnitPrice = unitPrice;
            Quantity = quantity;
            Notes = notes;
            EmployeeId = employeeId;
            PizzaSizeId = pizzaSizeId;
            PizzaCrustId = pizzaCrustId;
            PizzaEdgeId = pizzaEdgeId;
            OrderItemStatusId = OrderItemStatusIds.Lancado;
            TotalAmount = Math.Round(unitPrice * quantity, 2);
            IsActive = true;
            CreatedAt = Now;
        }
        internal static Result<OrderItem> Create(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime Now, long? initialStatusId = null)
        {
            if (quantity <= 0)
                return Result.Failure<OrderItem>(new Error("OrderItem.InvalidQuantity", "Quantity must be greater than zero."));
            if (unitPrice < 0)
                return Result.Failure<OrderItem>(new Error("OrderItem.InvalidUnitPrice", "Unit price cannot be negative."));

            var item = new OrderItem(customerOrderId, productId, unitPrice, quantity, notes, employeeId, Now);
            if (initialStatusId.HasValue && initialStatusId.Value != item.OrderItemStatusId)
                item.ApplyTransferredStatus(initialStatusId.Value, Now);

            return Result.Success(item);
        }
        internal static Result<OrderItem> CreatePizza(
            long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime Now,
            long pizzaSizeId, long? pizzaCrustId, long? pizzaEdgeId, IReadOnlyCollection<long> pizzaFlavorIds)
        {
            if (quantity <= 0)
                return Result.Failure<OrderItem>(new Error("OrderItem.InvalidQuantity", "Quantity must be greater than zero."));
            if (unitPrice < 0)
                return Result.Failure<OrderItem>(new Error("OrderItem.InvalidUnitPrice", "Unit price cannot be negative."));
            if (pizzaFlavorIds.Count == 0)
                return Result.Failure<OrderItem>(new Error("OrderItem.NoFlavorsSelected", "At least one pizza flavor must be selected."));

            var item = new OrderItem(customerOrderId, productId, unitPrice, quantity, notes, employeeId, Now, pizzaSizeId, pizzaCrustId, pizzaEdgeId);

            var fractionShare = Math.Round(1m / pizzaFlavorIds.Count, 4);
            foreach (var flavorId in pizzaFlavorIds)
            {
                var flavor = OrderItemPizzaFlavor.Create(item.Id, flavorId, fractionShare, Now);
                if (flavor.IsFailure)
                    return Result.Failure<OrderItem>(flavor.Error);

                item._pizzaFlavors.Add(flavor.Value);
            }

            return Result.Success(item);
        }
        internal Result ForceCancelForTransfer(long? actorEmployeeId, DateTime Now)
        {
            OrderItemStatusId = OrderItemStatusIds.Cancelado;
            CancelledByEmployeeId = actorEmployeeId;
            UpdatedAt = Now;
            return Result.Success();
        }
        /// <summary>
        /// Aplica, de forma direta e por referência, o status original de um item transferido de outra mesa/comanda.
        /// Usado só na criação do item de destino (via <see cref="Create"/>), por isso bypassa
        /// deliberadamente a validação de "status final" de <see cref="UpdateStatus"/> — o item acabou de
        /// nascer e ainda não tem Id persistido, então não pode (nem precisa) ser localizado de novo por Id.
        /// </summary>
        internal void ApplyTransferredStatus(long orderItemStatusId, DateTime Now)
        {
            OrderItemStatusId = orderItemStatusId;
            if (orderItemStatusId == OrderItemStatusIds.EnviadoCozinha) SentToKitchenAt = Now;
            if (orderItemStatusId == OrderItemStatusIds.Entregue) DeliveredAt = Now;
            UpdatedAt = Now;
        }
        internal Result UpdateStatus(long orderItemStatusId, long? actorEmployeeId, DateTime Now)
        {
            if (OrderItemStatusId is OrderItemStatusIds.Entregue or OrderItemStatusIds.Cancelado)
                return Result.Failure(new Error("OrderItem.FinalStatus", "Delivered or cancelled items cannot change status."));
            OrderItemStatusId = orderItemStatusId;
            if (orderItemStatusId == OrderItemStatusIds.EnviadoCozinha) SentToKitchenAt = Now;
            if (orderItemStatusId == OrderItemStatusIds.Entregue) DeliveredAt = Now;
            if (orderItemStatusId == OrderItemStatusIds.Cancelado) CancelledByEmployeeId = actorEmployeeId;

            UpdatedAt = Now;
            return Result.Success();
        }
        internal Result AddComplement(long complementId, decimal unitPriceCharged, DateTime Now)
        {
            if (OrderItemStatusId is OrderItemStatusIds.Entregue or OrderItemStatusIds.Cancelado)
                return Result.Failure(new Error("OrderItem.FinalStatus", "Delivered or cancelled items cannot be changed."));

            var complement = OrderItemComplement.Create(Id, complementId, unitPriceCharged, Now);
            if (complement.IsFailure)
                return Result.Failure(complement.Error);

            _complements.Add(complement.Value);
            RecalculateTotal();
            UpdatedAt = Now;
            return Result.Success();
        }
        internal Result RemoveComplement(long orderItemComplementId, DateTime Now)
        {
            var complement = _complements.FirstOrDefault(c => c.Id == orderItemComplementId && c.IsActive);
            if (complement is null)
                return Result.Failure(new Error("OrderItem.ComplementNotFound", "Order item complement not found."));

            complement.Deactivate(Now);
            RecalculateTotal();
            UpdatedAt = Now;
            return Result.Success();
        }
        public void Deactivate(DateTime Now)
        {
            IsActive = false;
            UpdatedAt = Now;
        }
        private void RecalculateTotal()
        {
            var complementsTotal = _complements.Where(c => c.IsActive).Sum(c => c.UnitPriceCharged);
            TotalAmount = Math.Round(UnitPrice * Quantity, 2) + complementsTotal;
        }
    }
}