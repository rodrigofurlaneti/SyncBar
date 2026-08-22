using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class UploadIFoodImageCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<UploadIFoodImageCommand, IFoodImageUploadResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodImageUploadResponse>> Handle(
        UploadIFoodImageCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UploadIFoodImageCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodImageUploadResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.UploadImageAsync(token, merchantId, request.JsonBody, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodImageUploadResponse>(new Error("IFoodCatalog.UploadImageFailed", result.ErrorMessage ?? "Falha ao enviar a imagem para o iFood."));

                return Result.Success(new IFoodImageUploadResponse(result.RawPayload));
            });
    }
}
