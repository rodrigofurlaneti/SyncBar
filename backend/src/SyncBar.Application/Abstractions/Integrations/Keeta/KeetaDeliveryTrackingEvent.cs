namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    /// <summary>
    /// Espelha o objeto "event" de DeliveryTrackingInfo/DeliveryTrackingInfoDisptach da API Keeta —
    /// usado tanto no despacho (dispatch) quanto nas atualizações de rastreio (tracking) do pedido.
    /// </summary>
    public sealed record KeetaDeliveryTrackingEvent(string Type, DateTime DateTimeUtc, string? Message = null);
}
