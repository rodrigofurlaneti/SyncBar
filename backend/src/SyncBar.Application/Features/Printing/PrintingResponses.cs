namespace SyncBar.Application.Features.Printing;

public sealed record PrinterResponse(
    long Id,
    string Name,
    int ConnectionType,
    string? PrinterName,
    string? IpAddress,
    int? Port,
    bool PrintsOrders,
    bool PrintsBills);

public sealed record PrintSettingsResponse(bool PrintOrdersEnabled, bool PrintBillsEnabled);
