using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodMerchantStatusByOperationQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantStatusByOperationQuery, IFoodMerchantStatusByOperationResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodMerchantStatusByOperationResponse>> Handle(
        GetIFoodMerchantStatusByOperationQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantStatusByOperationQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodMerchantStatusByOperationResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var status = await merchantClient.GetStatusByOperationAsync(token, merchantId, request.Operation, cancellationToken);
                if (!status.Success)
                    return Result.Failure<IFoodMerchantStatusByOperationResponse>(new Error("IFoodMerchant.StatusByOperationFailed", status.ErrorMessage ?? "Falha ao buscar o status da operação no iFood."));

                var validations = status.Validations
                    .Select(v => new IFoodMerchantValidationResponse(v.Id, v.State, v.Message))
                    .ToList();

                return Result.Success(new IFoodMerchantStatusByOperationResponse(
                    status.Operation, status.SalesChannel, status.Available, status.State, validations));
            });
    }
}
