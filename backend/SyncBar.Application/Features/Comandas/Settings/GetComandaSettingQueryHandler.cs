using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Comandas.Settings;

internal sealed class GetComandaSettingQueryHandler(IComandaSettingRepository settingRepository)
    : IQueryHandler<GetComandaSettingQuery, ComandaSettingResponse>
{
    public async Task<Result<ComandaSettingResponse>> Handle(
        GetComandaSettingQuery request, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByBranchAsync(request.BranchId, cancellationToken);
        // Sem configuracao: comandas sem limite (0 = ilimitado na exibicao).
        return Result.Success(new ComandaSettingResponse(setting?.DefaultLimitAmount ?? 0));
    }
}
