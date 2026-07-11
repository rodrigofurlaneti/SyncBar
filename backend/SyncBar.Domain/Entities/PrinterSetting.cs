using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class PrinterSetting : AggregateRoot
{
    public long BranchId { get; private set; }
    public bool PrintOrdersEnabled { get; private set; }
    public bool PrintBillsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PrinterSetting() : base(0) { }

    private PrinterSetting(long branchId, bool printOrdersEnabled, bool printBillsEnabled) : base(0)
    {
        BranchId = branchId;
        PrintOrdersEnabled = printOrdersEnabled;
        PrintBillsEnabled = printBillsEnabled;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<PrinterSetting> Create(long branchId, bool printOrdersEnabled, bool printBillsEnabled)
        => Result.Success(new PrinterSetting(branchId, printOrdersEnabled, printBillsEnabled));

    public void Update(bool printOrdersEnabled, bool printBillsEnabled)
    {
        PrintOrdersEnabled = printOrdersEnabled;
        PrintBillsEnabled = printBillsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
