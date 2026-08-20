namespace SyncBar.Domain.Constants;

// Status da entrega do LADO DA LOGÍSTICA PRÓPRIA (frota própria) — máquina de estados separada
// do IFoodOrderStatuses (que é sobre confirmação/preparo perante o iFood) e do OrderStatusIds
// (cozinha/pagamento, interno do SyncBar). Ciclo de vida documentado no módulo Logistics:
// DRIVER_ASSIGNED → GOING_TO_ORIGIN → ARRIVED_AT_ORIGIN → DISPATCHED → ARRIVED_AT_DESTINATION →
// DELIVERY_CODE_VERIFIED (esta última só quando o pedido não é self-delivery — ver ressalva em
// IIFoodLogisticsClient/verifyDeliveryCode, que pode devolver 412 nesse caso).
public static class IFoodLogisticsStatuses
{
    public const string DriverAssigned = "DRIVER_ASSIGNED";
    public const string GoingToOrigin = "GOING_TO_ORIGIN";
    public const string ArrivedAtOrigin = "ARRIVED_AT_ORIGIN";
    public const string Dispatched = "DISPATCHED";
    public const string ArrivedAtDestination = "ARRIVED_AT_DESTINATION";
    public const string DeliveryCodeVerified = "DELIVERY_CODE_VERIFIED";
}
