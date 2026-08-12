using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.SetSettings;

internal sealed class SetPrintSettingsCommandHandler(
    IPrinterSettingRepository settingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetPrintSettingsCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetPrintSettingsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetPrintSettingsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou administrador executando a ação, preencha:
                // userIdBox.Value = request.UserId;

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
            });
    }
}