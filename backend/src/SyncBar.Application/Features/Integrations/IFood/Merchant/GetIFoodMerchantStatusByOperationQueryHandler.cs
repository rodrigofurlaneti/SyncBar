using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

internal sealed class GetIfoodMerchantStatusByOperationQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodMerchantStatusByOperationQuery, IfoodMerchantStatusByOperationResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodMerchantStatusByOperationResponse>> Handle(
        GetIfoodMerchantStatusByOperationQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodMerchantStatusByOperationQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodMerchantStatusByOperationResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var status = await merchantClient.GetStatusByOperationAsync(token, merchantId, request.Operation, cancellationToken);
                if (!status.Success)
                    return Result.Failure<IfoodMerchantStatusByOperationResponse>(new Error("IfoodMerchant.StatusByOperationFailed", status.ErrorMessage ?? "Falha ao buscar o status da operação no Ifood."));

                var validations = status.Validations
                    .Select(v => new IfoodMerchantValidationResponse(v.Id, v.State, v.Message))
                    .ToList();

                return Result.Success(new IfoodMerchantStatusByOperationResponse(
                    status.Operation, status.SalesChannel, status.Available, status.State, validations));
            });
    }
}
