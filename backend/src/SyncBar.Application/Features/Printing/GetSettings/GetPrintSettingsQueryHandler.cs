using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.GetSettings;

internal sealed class GetPrintSettingsQueryHandler(
    IPrinterSettingRepository settingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetPrintSettingsQuery, PrintSettingsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<PrintSettingsResponse>> Handle(
        GetPrintSettingsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetPrintSettingsQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/sistema que está consultando, preencha:

                var settings = await settingRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                // Sem registro: impressao ligada por padrao.
                return Result.Success(settings is null
                    ? new PrintSettingsResponse(true, true)
                    : new PrintSettingsResponse(settings.PrintOrdersEnabled, settings.PrintBillsEnabled));
            });
    }
}