using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

internal sealed class GetServiceFeeSettingQueryHandler(IServiceFeeSettingRepository settingRepository)
    : IQueryHandler<GetServiceFeeSettingQuery, ServiceFeeSettingResponse>
{
    public async Task<Result<ServiceFeeSettingResponse>> Handle(
        GetServiceFeeSettingQuery request, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByBranchAsync(request.BranchId, cancellationToken);
        // Sem configuracao: taxa LIGADA por padrao (cobra os 10%).
        return Result.Success(new ServiceFeeSettingResponse(setting?.Enabled ?? true));
    }
}
