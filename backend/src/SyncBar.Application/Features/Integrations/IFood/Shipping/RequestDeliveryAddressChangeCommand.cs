using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record RequestDeliveryAddressChangeCommand(
    long IfoodOrderId,
    string StreetNumber,
    string StreetName,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string? Country,
    string? Reference,
    double? Latitude,
    double? Longitude) : ICommand;
