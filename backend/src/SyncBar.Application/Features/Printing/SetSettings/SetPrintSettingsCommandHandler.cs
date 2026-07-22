using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.SetSettings;

internal sealed class SetPrintSettingsCommandHandler(
    IPrinterSettingRepository settingRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetPrintSettingsCommand>
{
    public async Task<Result> Handle(SetPrintSettingsCommand request, CancellationToken cancellationToken)
    {
        // Upsert por filial (espelha UQ_PrinterSetting_BranchId filtrado).
        var settings = await settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
        if (settings is null)
        {
            var created = PrinterSetting.Create(request.BranchId, request.PrintOrdersEnabled, request.PrintBillsEnabled);
            if (created.IsFailure)
                return Result.Failure(created.Error);
            await settingRepository.AddAsync(created.Value, cancellationToken);
        }
        else
        {
            settings.Update(request.PrintOrdersEnabled, request.PrintBillsEnabled);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
