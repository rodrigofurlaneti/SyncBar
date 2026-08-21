using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodMerchantDetailsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantDetailsQuery, IFoodMerchantDetailsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodMerchantDetailsResponse>> Handle(
        GetIFoodMerchantDetailsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantDetailsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodMerchantDetailsResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var details = await merchantClient.GetMerchantDetailsAsync(token, merchantId, cancellationToken);
                if (!details.Success)
                    return Result.Failure<IFoodMerchantDetailsResponse>(new Error("IFoodMerchant.DetailsFailed", details.ErrorMessage ?? "Falha ao buscar os detalhes da loja no iFood."));

                var address = details.Address is null
                    ? null
                    : new IFoodMerchantAddressResponse(
                        details.Address.Country, details.Address.State, details.Address.City, details.Address.PostalCode,
                        details.Address.District, details.Address.Street, details.Address.Number,
                        details.Address.Latitude, details.Address.Longitude);

                return Result.Success(new IFoodMerchantDetailsResponse(
                    details.Id, details.Name, details.CorporateName, details.Description, details.Type, details.Status,
                    details.CreatedAt, address));
            });
    }
}
