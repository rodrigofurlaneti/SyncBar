using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.V1Legacy;

internal sealed class InvokeIFoodCatalogV1OperationCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<InvokeIFoodCatalogV1OperationCommand, IFoodCatalogV1OperationResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCatalogV1OperationResponse>> Handle(
        InvokeIFoodCatalogV1OperationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(InvokeIFoodCatalogV1OperationCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCatalogV1OperationResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.InvokeCatalogV1Async(
                    token, merchantId, request.Operation, request.RouteParams, request.QueryParams, request.JsonBody, cancellationToken);

                // Diferente do resto do módulo, aqui o "sucesso" da chamada MediatR não depende do
                // sucesso HTTP — a resposta (inclusive erro do iFood) é sempre repassada ao
                // chamador, que decide o que fazer com ela (console de admin cru, ver
                // IFoodCatalogAdvancedPage.tsx). Só falha o Result se a própria chamada não
                // conseguiu nem ser feita (StatusCode == 0, exceção local).
                if (!result.Success && result.StatusCode == 0)
                    return Result.Failure<IFoodCatalogV1OperationResponse>(new Error("IFoodCatalog.V1InvokeFailed", result.ErrorMessage ?? "Falha ao chamar o endpoint legado (v1) do iFood."));

                return Result.Success(new IFoodCatalogV1OperationResponse(result.Success, result.StatusCode, result.ResponseBody, result.ErrorMessage));
            });
    }
}
