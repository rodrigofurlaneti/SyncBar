using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodInterruptionsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodInterruptionsQuery, IReadOnlyCollection<IFoodInterruptionResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodInterruptionResponse>>> Handle(
        GetIFoodInterruptionsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodInterruptionsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodInterruptionResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await merchantClient.GetInterruptionsAsync(token, merchantId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodInterruptionResponse>>(new Error("IFoodMerchant.InterruptionsFailed", result.ErrorMessage ?? "Failed to fetch interruptions from iFood."));

                IReadOnlyCollection<IFoodInterruptionResponse> response = result.Interruptions
                    .Select(i => new IFoodInterruptionResponse(i.Id, i.Description, i.Start, i.End))
                    .ToList();

                return Result.Success(response);
            });
    }
}
