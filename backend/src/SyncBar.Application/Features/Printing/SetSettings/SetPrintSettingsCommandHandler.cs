using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.SetSettings;

internal sealed class SetPrintSettingsCommandHandler : BaseCommandHandler<SetPrintSettingsCommand>
{
    private readonly IPrinterSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetPrintSettingsCommandHandler(
        IPrinterSettingRepository settingRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetPrintSettingsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetPrintSettingsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou administrador executando a ação, preencha:

                // Upsert por filial (espelha UQ_PrinterSetting_BranchId filtrado).
                var settings = await _settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                if (settings is null)
                {
                    var created = PrinterSetting.Create(request.BranchId, request.PrintOrdersEnabled, request.PrintBillsEnabled);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    await _settingRepository.AddAsync(created.Value, cancellationToken);
                }
                else
                {
                    settings.Update(request.PrintOrdersEnabled, request.PrintBillsEnabled);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}