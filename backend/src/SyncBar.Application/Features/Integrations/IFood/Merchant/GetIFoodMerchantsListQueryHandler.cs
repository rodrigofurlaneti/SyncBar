using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodMerchantsListQueryHandler(
    IIFoodTokenProvider tokenProvider,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantsListQuery, IReadOnlyCollection<IFoodMerchantSummaryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodMerchantSummaryResponse>>> Handle(
        GetIFoodMerchantsListQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantsListQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var token = await tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IReadOnlyCollection<IFoodMerchantSummaryResponse>>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var result = await merchantClient.ListMerchantsAsync(token, request.Page, request.Size, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodMerchantSummaryResponse>>(new Error("IFoodMerchant.ListFailed", result.ErrorMessage ?? "Falha ao listar as lojas no iFood."));

                IReadOnlyCollection<IFoodMerchantSummaryResponse> merchants = result.Merchants
                    .Select(m => new IFoodMerchantSummaryResponse(m.Id, m.Name, m.CorporateName))
                    .ToList();

                return Result.Success(merchants);
            });
    }
}
