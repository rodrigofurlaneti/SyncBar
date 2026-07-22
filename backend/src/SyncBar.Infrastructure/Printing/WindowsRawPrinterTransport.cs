using System.Runtime.InteropServices;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Printing;

// Envia bytes RAW para a fila de impressao do Windows (driver instalado, ex.: "ELGIN i9(USB)").
internal sealed class WindowsRawPrinterTransport : IRawPrinterTransport
{
    public bool CanHandle(Printer printer) => printer.ConnectionType == Printer.ConnectionWindows;

    public Task SendAsync(Printer printer, byte[] payload, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException("Impressão via driver USB só está disponível no Windows.");

        if (!OpenPrinter(printer.PrinterName!, out var handle, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Impressora \"{printer.PrinterName}\" não encontrada no Windows. Confira o nome exato em Configurações > Impressoras.");

        try
        {
            var docInfo = new DOCINFOA { pDocName = "SyncBar", pDataType = "RAW" };
            if (!StartDocPrinter(handle, 1, docInfo))
                throw new InvalidOperationException("Falha ao iniciar o documento na fila de impressão.");
            try
            {
                StartPagePrinter(handle);
                var unmanaged = Marshal.AllocHGlobal(payload.Length);
                try
                {
                    Marshal.Copy(payload, 0, unmanaged, payload.Length);
                    if (!WritePrinter(handle, unmanaged, payload.Length, out _))
                        throw new InvalidOperationException("Falha ao enviar dados para a impressora.");
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanaged);
                }
                EndPagePrinter(handle);
            }
            finally
            {
                EndDocPrinter(handle);
            }
        }
        finally
        {
            ClosePrinter(handle);
        }

        return Task.CompletedTask;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string? pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
