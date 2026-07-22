using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Printing.SetSettings;

public sealed record SetPrintSettingsCommand(
    long BranchId,
    bool PrintOrdersEnabled,
    bool PrintBillsEnabled) : ICommand;
