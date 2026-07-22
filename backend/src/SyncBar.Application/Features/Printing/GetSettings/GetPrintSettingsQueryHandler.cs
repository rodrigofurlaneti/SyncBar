using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Printing.GetSettings;

internal sealed class GetPrintSettingsQueryHandler(IPrinterSettingRepository settingRepository)
    : IQueryHandler<GetPrintSettingsQuery, PrintSettingsResponse>
{
    public async Task<Result<PrintSettingsResponse>> Handle(
        GetPrintSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        // Sem registro: impressao ligada por padrao.
        return Result.Success(settings is null
            ? new PrintSettingsResponse(true, true)
            : new PrintSettingsResponse(settings.PrintOrdersEnabled, settings.PrintBillsEnabled));
    }
}
