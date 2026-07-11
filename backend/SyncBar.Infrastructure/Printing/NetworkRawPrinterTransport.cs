using System.Net.Sockets;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Printing;

// Impressoras de rede: bytes RAW direto no socket TCP (padrao porta 9100).
internal sealed class NetworkRawPrinterTransport : IRawPrinterTransport
{
    public bool CanHandle(Printer printer) => printer.ConnectionType == Printer.ConnectionNetwork;

    public async Task SendAsync(Printer printer, byte[] payload, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await client.ConnectAsync(printer.IpAddress!, printer.Port!.Value, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(payload, timeout.Token);
            await stream.FlushAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Impressora de rede {printer.IpAddress}:{printer.Port} não respondeu em 5s.");
        }
    }
}
