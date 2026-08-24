using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.CreatePrinter;

internal sealed class CreatePrinterCommandHandler : BaseCommandHandler<CreatePrinterCommand, long>
{
    private readonly IPrinterRepository _printerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePrinterCommandHandler(
        IPrinterRepository printerRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _printerRepository = printerRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(CreatePrinterCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreatePrinterCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está cadastrando a impressora, preencha:

                var printer = Printer.Create(
                    request.BranchId, request.Name.Trim(), request.ConnectionType,
                    request.PrinterName?.Trim(), request.IpAddress?.Trim(), request.Port,
                    request.PrintsOrders, request.PrintsBills);

                if (printer.IsFailure)
                    return Result.Failure<long>(printer.Error);

                await _printerRepository.AddAsync(printer.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(printer.Value.Id);
            });
    }
}