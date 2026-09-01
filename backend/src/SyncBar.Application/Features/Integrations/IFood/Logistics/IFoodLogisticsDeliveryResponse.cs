namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed record IfoodLogisticsDeliveryResponse(
    long Id,
    long IfoodOrderId,
    string? IfoodOrderDisplayId,
    string DriverName,
    string DriverPhone,
    string DriverVehicleType,
    string Status,
    string? CustomerName,
    string? DeliveryAddress,
    DateTime AssignedAt,
    DateTime? GoingToOriginAt,
    DateTime? ArrivedAtOriginAt,
    DateTime? DispatchedAt,
    DateTime? ArrivedAtDestinationAt,
    DateTime? DeliveryCodeVerifiedAt);
