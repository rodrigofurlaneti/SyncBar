using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

internal sealed class UploadIfoodImageCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<UploadIfoodImageCommand, IfoodImageUploadResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodImageUploadResponse>> Handle(
        UploadIfoodImageCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UploadIfoodImageCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodImageUploadResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.UploadImageAsync(token, merchantId, request.JsonBody, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodImageUploadResponse>(new Error("IfoodCatalog.UploadImageFailed", result.ErrorMessage ?? "Falha ao enviar a imagem para o Ifood."));

                return Result.Success(new IfoodImageUploadResponse(result.RawPayload));
            });
    }
}
