using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.CreatePrinter;

internal sealed class CreatePrinterCommandHandler(
    IPrinterRepository printerRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePrinterCommand, long>
{
    public async Task<Result<long>> Handle(CreatePrinterCommand request, CancellationToken cancellationToken)
    {
        var printer = Printer.Create(
            request.BranchId, request.Name.Trim(), request.ConnectionType,
            request.PrinterName?.Trim(), request.IpAddress?.Trim(), request.Port,
            request.PrintsOrders, request.PrintsBills);
        if (printer.IsFailure)
            return Result.Failure<long>(printer.Error);

        await printerRepository.AddAsync(printer.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(printer.Value.Id);
    }
}
