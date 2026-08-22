using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodMerchantStatusQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantStatusQuery, IFoodMerchantStatusResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodMerchantStatusResponse>> Handle(
        GetIFoodMerchantStatusQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantStatusQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodMerchantStatusResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var status = await merchantClient.GetStatusAsync(token, merchantId, cancellationToken);
                if (!status.Success)
                    return Result.Failure<IFoodMerchantStatusResponse>(new Error("IFoodMerchant.StatusFailed", status.ErrorMessage ?? "Failed to fetch status from iFood."));

                var validations = status.Validations
                    .Select(v => new IFoodMerchantValidationResponse(v.Id, v.State, v.Message))
                    .ToList();

                return Result.Success(new IFoodMerchantStatusResponse(status.OperationState, status.Available, validations));
            });
    }
}
