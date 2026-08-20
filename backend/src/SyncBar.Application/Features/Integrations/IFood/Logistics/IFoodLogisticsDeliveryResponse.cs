namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed record IFoodLogisticsDeliveryResponse(
    long Id,
    long IFoodOrderId,
    string? IFoodOrderDisplayId,
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
