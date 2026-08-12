using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.DeactivatePrinter;

internal sealed class DeactivatePrinterCommandHandler(
    IPrinterRepository printerRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeactivatePrinterCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeactivatePrinterCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivatePrinterCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário responsável pela desativação, preencha:
                // userIdBox.Value = request.UserId;

                var printer = await printerRepository.GetByIdForUpdateAsync(request.PrinterId, cancellationToken);
                if (printer is null || !printer.IsActive)
                    return Result.Failure(new Error("Printer.NotFound", "Printer not found."));

                printer.Deactivate();
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}