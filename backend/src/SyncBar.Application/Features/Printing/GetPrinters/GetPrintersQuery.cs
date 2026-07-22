using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Printing.GetPrinters;

public sealed record GetPrintersQuery(long BranchId) : IQuery<IReadOnlyCollection<PrinterResponse>>;
