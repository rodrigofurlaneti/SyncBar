using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record RequestDeliveryAddressChangeCommand(
    long IFoodOrderId,
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
