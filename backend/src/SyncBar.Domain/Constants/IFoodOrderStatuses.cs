namespace SyncBar.Domain.Constants;

// Status do pedido do LADO DO Ifood (não confundir com OrderStatusIds, que é a máquina de
// estados interna do SyncBar de cozinha/pagamento). Valores batem com o ciclo de vida
// documentado no módulo Order do Ifood (categoria FOOD): PLACED → CONFIRMED →
// PREPARATION_STARTED (opcional) → DISPATCHED/READY_TO_PICKUP → CONCLUDED, ou CANCELLED
// a qualquer momento antes de CONCLUDED.
public static class IfoodOrderStatuses
{
    public const string Placed = "PLACED";
    public const string Confirmed = "CONFIRMED";
    public const string PreparationStarted = "PREPARATION_STARTED";
    public const string ReadyToPickup = "READY_TO_PICKUP";
    public const string Dispatched = "DISPATCHED";
    public const string Concluded = "CONCLUDED";
    public const string Cancelled = "CANCELLED";
    public const string CancellationRequested = "CANCELLATION_REQUESTED";


}
