using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Printing.CreatePrinter;

public sealed record CreatePrinterCommand(
    long BranchId,
    string Name,
    int ConnectionType,
    string? PrinterName,
    string? IpAddress,
    int? Port,
    bool PrintsOrders,
    bool PrintsBills) : ICommand<long>;
