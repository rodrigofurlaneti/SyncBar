using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.GetPrinters;

internal sealed class GetPrintersQueryHandler(
    IPrinterRepository printerRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetPrintersQuery, IReadOnlyCollection<PrinterResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<PrinterResponse>>> Handle(
        GetPrintersQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetPrintersQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está consultando, preencha:

                var printers = await printerRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<PrinterResponse> response = printers
                    .OrderBy(p => p.Name)
                    .Select(p => new PrinterResponse(
                        p.Id, p.Name, p.ConnectionType, p.PrinterName, p.IpAddress, p.Port,
                        p.PrintsOrders, p.PrintsBills))
                    .ToList();

                return Result.Success(response);
            });
    }
}