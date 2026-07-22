using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Printing;

internal interface IRawPrinterTransport
{
    bool CanHandle(Printer printer);
    Task SendAsync(Printer printer, byte[] payload, CancellationToken cancellationToken);
}
