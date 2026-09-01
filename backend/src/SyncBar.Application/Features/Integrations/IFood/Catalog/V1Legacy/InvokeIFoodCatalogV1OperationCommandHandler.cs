using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.V1Legacy;

internal sealed class InvokeIfoodCatalogV1OperationCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<InvokeIfoodCatalogV1OperationCommand, IfoodCatalogV1OperationResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodCatalogV1OperationResponse>> Handle(
        InvokeIfoodCatalogV1OperationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(InvokeIfoodCatalogV1OperationCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodCatalogV1OperationResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.InvokeCatalogV1Async(
                    token, merchantId, request.Operation, request.RouteParams, request.QueryParams, request.JsonBody, cancellationToken);

                // Diferente do resto do módulo, aqui o "sucesso" da chamada MediatR não depende do
                // sucesso HTTP — a resposta (inclusive erro do Ifood) é sempre repassada ao
                // chamador, que decide o que fazer com ela (console de admin cru, ver
                // IfoodCatalogAdvancedPage.tsx). Só falha o Result se a própria chamada não
                // conseguiu nem ser feita (StatusCode == 0, exceção local).
                if (!result.Success && result.StatusCode == 0)
                    return Result.Failure<IfoodCatalogV1OperationResponse>(new Error("IfoodCatalog.V1InvokeFailed", result.ErrorMessage ?? "Falha ao chamar o endpoint legado (v1) do Ifood."));

                return Result.Success(new IfoodCatalogV1OperationResponse(result.Success, result.StatusCode, result.ResponseBody, result.ErrorMessage));
            });
    }
}
