using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

/// <summary>
/// Liga um <see cref="CustomerOrder"/> (o pedido "de verdade" no SyncBar — cozinha,
/// faturamento) ao pedido correspondente no iFood. Guarda o status do LADO DO IFOOD, que é uma
/// máquina de estados diferente do <c>OrderStatusId</c> do SyncBar (aquele é sobre
/// cozinha/pagamento; este é sobre confirmação/preparo/despacho perante o iFood — SLA de 8
/// minutos pra confirmar, depois startPreparation/readyToPickup/dispatch).
/// </summary>
public sealed class IFoodOrder : AggregateRoot
{
    public long CustomerOrderId { get; private set; }
    public long BranchId { get; private set; }
    public string IFoodOrderId { get; private set; } = null!;
    public string? DisplayId { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public string IFoodOrderType { get; private set; } = null!; // DELIVERY / TAKEOUT / DINE_IN (bruto do iFood)
    public string Status { get; private set; } = null!;
    // Quem entrega o pedido, bruto do iFood (delivery.deliveredBy) — "IFOOD" quando é a logística
    // do próprio iFood; qualquer outro valor (ex.: "MERCHANT") indica self-delivery/frota
    // própria, elegível pro fluxo de Logística (fase 7, ver IFoodLogisticsDelivery). Nulo para
    // pedidos TAKEOUT/DINE_IN (sem entrega) ou quando o iFood não informou o campo.
    // Ressalva de confiança: nome do valor "IFOOD" assumido pela doc de Logistics/Order — não
    // há uma lista fechada de valores possíveis documentada explicitamente pelo iFood.
    public string? DeliveredBy { get; private set; }
    public DateTime ConfirmDeadlineAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    // Algum item do pedido não bateu com nenhum Product do catálogo (por EAN/código de barras)
    // — pedido foi aceito mesmo assim (SLA de 8 min não espera reconciliação manual), mas fica
    // sinalizado aqui pra equipe conferir/ajustar o pedido na tela normal de Pedidos.
    public bool HasUnmappedItems { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodOrder() : base(0) { }

    private IFoodOrder(
        long customerOrderId, long branchId, string ifoodOrderId, string? displayId, string merchantId,
        string ifoodOrderType, string? deliveredBy, DateTime now, bool hasUnmappedItems) : base(0)
    {
        CustomerOrderId = customerOrderId;
        BranchId = branchId;
        IFoodOrderId = ifoodOrderId;
        DisplayId = displayId;
        MerchantId = merchantId;
        IFoodOrderType = ifoodOrderType;
        DeliveredBy = deliveredBy;
        Status = IFoodOrderStatuses.Placed;
        // SLA oficial: confirmar em até 8 minutos. Pedidos agendados usam preparationStartDateTime
        // como referência oficial — não diferenciado nesta fase porque o fluxo essencial confirma
        // automaticamente assim que cria o pedido, então o prazo nunca chega a ser testado.
        ConfirmDeadlineAt = now.AddMinutes(8);
        HasUnmappedItems = hasUnmappedItems;
        IsActive = true;
        CreatedAt = now;
    }

    public static Result<IFoodOrder> Create(
        long customerOrderId, long branchId, string ifoodOrderId, string? displayId, string merchantId,
        string ifoodOrderType, string? deliveredBy, DateTime now, bool hasUnmappedItems)
    {
        if (string.IsNullOrWhiteSpace(ifoodOrderId))
            return Result.Failure<IFoodOrder>(new Error("IFoodOrder.MissingId", "iFood order id is required."));
        if (string.IsNullOrWhiteSpace(merchantId))
            return Result.Failure<IFoodOrder>(new Error("IFoodOrder.MissingMerchantId", "Merchant id is required."));

        return Result.Success(new IFoodOrder(
            customerOrderId, branchId, ifoodOrderId, displayId, merchantId, ifoodOrderType, deliveredBy, now, hasUnmappedItems));
    }

    public void MarkConfirmed(DateTime now)
    {
        Status = IFoodOrderStatuses.Confirmed;
        ConfirmedAt = now;
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTime now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
