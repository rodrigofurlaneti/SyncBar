using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.GetPrinters;

internal sealed class GetPrintersQueryHandler(IPrinterRepository printerRepository)
    : IQueryHandler<GetPrintersQuery, IReadOnlyCollection<PrinterResponse>>
{
    public async Task<Result<IReadOnlyCollection<PrinterResponse>>> Handle(
        GetPrintersQuery request, CancellationToken cancellationToken)
    {
        var printers = await printerRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        IReadOnlyCollection<PrinterResponse> response = printers
            .OrderBy(p => p.Name)
            .Select(p => new PrinterResponse(
                p.Id, p.Name, p.ConnectionType, p.PrinterName, p.IpAddress, p.Port,
                p.PrintsOrders, p.PrintsBills))
            .ToList();

        return Result.Success(response);
    }
}
