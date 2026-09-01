using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

/// <summary>
/// Liga um <see cref="CustomerOrder"/> (o pedido "de verdade" no SyncBar — cozinha,
/// faturamento) ao pedido correspondente no Ifood. Guarda o status do LADO DO Ifood, que é uma
/// máquina de estados diferente do <c>OrderStatusId</c> do SyncBar (aquele é sobre
/// cozinha/pagamento; este é sobre confirmação/preparo/despacho perante o Ifood — SLA de 8
/// minutos pra confirmar, depois startPreparation/readyToPickup/dispatch).
/// </summary>
public sealed class IfoodOrder : AggregateRoot
{
    public long CustomerOrderId { get; private set; }
    public long BranchId { get; private set; }
    public string IfoodOrderId { get; private set; } = null!;
    public string? DisplayId { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public string IfoodOrderType { get; private set; } = null!; // DELIVERY / TAKEOUT / DINE_IN (bruto do Ifood)
    public string Status { get; private set; } = null!;
    // Quem entrega o pedido, bruto do Ifood (delivery.deliveredBy) — "Ifood" quando é a logística
    // do próprio Ifood; qualquer outro valor (ex.: "MERCHANT") indica self-delivery/frota
    // própria, elegível pro fluxo de Logística (fase 7, ver IfoodLogisticsDelivery). Nulo para
    // pedidos TAKEOUT/DINE_IN (sem entrega) ou quando o Ifood não informou o campo.
    // Ressalva de confiança: nome do valor "Ifood" assumido pela doc de Logistics/Order — não
    // há uma lista fechada de valores possíveis documentada explicitamente pelo Ifood.
    public string? DeliveredBy { get; private set; }
    // Fase 14 — antes buscado da API (IfoodOrderDetailsDto) e descartado sem persistir. "IMMEDIATE"
    // (padrão) ou "SCHEDULED"; PreparationStartDateTime só é preenchido quando OrderTiming é
    // "SCHEDULED". Guardado agora pra tela de Pedidos poder mostrar "Agendado para HH:mm" — a SLA
    // de confirmação de 8 minutos (ConfirmDeadlineAt) continua contada a partir de agora mesmo
    // pra pedido agendado, sem mudança de comportamento: o fluxo essencial já confirma
    // automaticamente assim que o pedido chega, e não há doc oficial confirmando se o Ifood espera
    // um prazo de confirmação diferente pra pedido agendado — ver Ifood-integration-status.md.
    public string OrderTiming { get; private set; } = "IMMEDIATE";
    public DateTime? PreparationStartDateTime { get; private set; }
    public DateTime ConfirmDeadlineAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    // Algum item do pedido não bateu com nenhum Product do catálogo (por EAN/código de barras)
    // — pedido foi aceito mesmo assim (SLA de 8 min não espera reconciliação manual), mas fica
    // sinalizado aqui pra equipe conferir/ajustar o pedido na tela normal de Pedidos.
    public bool HasUnmappedItems { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodOrder() : base(0) { }

    private IfoodOrder(
        long customerOrderId, long branchId, string IfoodOrderId, string? displayId, string merchantId,
        string IfoodOrderType, string? deliveredBy, string orderTiming, DateTime? preparationStartDateTime,
        DateTime now, bool hasUnmappedItems) : base(0)
    {
        CustomerOrderId = customerOrderId;
        BranchId = branchId;
        IfoodOrderId = IfoodOrderId;
        DisplayId = displayId;
        MerchantId = merchantId;
        IfoodOrderType = IfoodOrderType;
        DeliveredBy = deliveredBy;
        OrderTiming = string.IsNullOrWhiteSpace(orderTiming) ? "IMMEDIATE" : orderTiming;
        PreparationStartDateTime = preparationStartDateTime;
        Status = IfoodOrderStatuses.Placed;
        // SLA oficial: confirmar em até 8 minutos. Pedidos agendados usam preparationStartDateTime
        // como referência oficial — não diferenciado nesta fase porque o fluxo essencial confirma
        // automaticamente assim que cria o pedido, então o prazo nunca chega a ser testado.
        ConfirmDeadlineAt = now.AddMinutes(8);
        HasUnmappedItems = hasUnmappedItems;
        IsActive = true;
        CreatedAt = now;
    }

    public static Result<IfoodOrder> Create(
        long customerOrderId, long branchId, string IfoodOrderId, string? displayId, string merchantId,
        string IfoodOrderType, string? deliveredBy, string orderTiming, DateTime? preparationStartDateTime,
        DateTime now, bool hasUnmappedItems)
    {
        if (string.IsNullOrWhiteSpace(IfoodOrderId))
            return Result.Failure<IfoodOrder>(new Error("IfoodOrder.MissingId", "Ifood order id is required."));
        if (string.IsNullOrWhiteSpace(merchantId))
            return Result.Failure<IfoodOrder>(new Error("IfoodOrder.MissingMerchantId", "Merchant id is required."));

        return Result.Success(new IfoodOrder(
            customerOrderId, branchId, IfoodOrderId, displayId, merchantId, IfoodOrderType, deliveredBy,
            orderTiming, preparationStartDateTime, now, hasUnmappedItems));
    }

    public void MarkConfirmed(DateTime now)
    {
        Status = IfoodOrderStatuses.Confirmed;
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
