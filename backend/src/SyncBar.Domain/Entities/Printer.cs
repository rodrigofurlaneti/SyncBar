using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Printer : AggregateRoot
{
    public const int ConnectionWindows = 1; // USB/driver instalado no Windows
    public const int ConnectionNetwork = 2; // TCP raw (porta 9100)

    public long BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public int ConnectionType { get; private set; }
    public string? PrinterName { get; private set; }
    public string? IpAddress { get; private set; }
    public int? Port { get; private set; }
    public bool PrintsOrders { get; private set; }
    public bool PrintsBills { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Printer() : base(0) { }

    private Printer(long branchId, string name, int connectionType, string? printerName,
        string? ipAddress, int? port, bool printsOrders, bool printsBills) : base(0)
    {
        BranchId = branchId;
        Name = name;
        ConnectionType = connectionType;
        PrinterName = printerName;
        IpAddress = ipAddress;
        Port = port;
        PrintsOrders = printsOrders;
        PrintsBills = printsBills;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Printer> Create(long branchId, string name, int connectionType,
        string? printerName, string? ipAddress, int? port, bool printsOrders, bool printsBills)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Printer>(new Error("Printer.EmptyName", "Name is required."));
        if (connectionType is not (ConnectionWindows or ConnectionNetwork))
            return Result.Failure<Printer>(new Error("Printer.InvalidConnection", "Connection type must be Windows (1) or Network (2)."));
        if (connectionType == ConnectionWindows && string.IsNullOrWhiteSpace(printerName))
            return Result.Failure<Printer>(new Error("Printer.MissingDriver", "Windows printer requires the installed driver name."));
        if (connectionType == ConnectionNetwork && (string.IsNullOrWhiteSpace(ipAddress) || port is null or <= 0 or > 65535))
            return Result.Failure<Printer>(new Error("Printer.MissingAddress", "Network printer requires IP address and port."));
        if (!printsOrders && !printsBills)
            return Result.Failure<Printer>(new Error("Printer.NoRole", "Printer must print orders, bills or both."));

        return Result.Success(new Printer(branchId, name, connectionType, printerName, ipAddress, port, printsOrders, printsBills));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
