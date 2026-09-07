using System.Runtime.InteropServices;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Printing;

// Envia bytes RAW para a fila de impressao do Windows (driver instalado, ex.: "ELGIN i9(USB)").
internal sealed partial class WindowsRawPrinterTransport : IRawPrinterTransport
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

    [LibraryImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [LibraryImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClosePrinter(IntPtr hPrinter);

    // StartDocPrinter continua em [DllImport] clássico (não convertido para LibraryImport): o
    // gerador de P/Invoke por código-fonte não sabe marshalar uma classe com campos [MarshalAs]
    // (SYSLIB1051) — só suporta isso automaticamente via marshalling COM (o "modo legado"). Migrar
    // exigiria reescrever DOCINFOA para structs de ponteiros com marshalling manual das 3 strings
    // (Marshal.StringToHGlobalAnsi + free), o que é um risco desnecessário para um caminho de
    // código sem cobertura de teste automatizada e só verificável contra uma impressora física real.
    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [LibraryImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EndDocPrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool StartPagePrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EndPagePrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
