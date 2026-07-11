using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Printing.DeactivatePrinter;

public sealed record DeactivatePrinterCommand(long PrinterId) : ICommand;
