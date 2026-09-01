using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

internal sealed class GetIfoodMerchantDetailsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodMerchantDetailsQuery, IfoodMerchantDetailsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodMerchantDetailsResponse>> Handle(
        GetIfoodMerchantDetailsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodMerchantDetailsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodMerchantDetailsResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var details = await merchantClient.GetMerchantDetailsAsync(token, merchantId, cancellationToken);
                if (!details.Success)
                    return Result.Failure<IfoodMerchantDetailsResponse>(new Error("IfoodMerchant.DetailsFailed", details.ErrorMessage ?? "Falha ao buscar os detalhes da loja no Ifood."));

                var address = details.Address is null
                    ? null
                    : new IfoodMerchantAddressResponse(
                        details.Address.Country, details.Address.State, details.Address.City, details.Address.PostalCode,
                        details.Address.District, details.Address.Street, details.Address.Number,
                        details.Address.Latitude, details.Address.Longitude);

                return Result.Success(new IfoodMerchantDetailsResponse(
                    details.Id, details.Name, details.CorporateName, details.Description, details.Type, details.Status,
                    details.CreatedAt, address));
            });
    }
}
