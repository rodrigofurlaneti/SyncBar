using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.DeactivatePrinter;

internal sealed class DeactivatePrinterCommandHandler(
    IPrinterRepository printerRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivatePrinterCommand>
{
    public async Task<Result> Handle(DeactivatePrinterCommand request, CancellationToken cancellationToken)
    {
        var printer = await printerRepository.GetByIdForUpdateAsync(request.PrinterId, cancellationToken);
        if (printer is null || !printer.IsActive)
            return Result.Failure(new Error("Printer.NotFound", "Printer not found."));

        printer.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
