using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

internal sealed class GetIfoodMerchantsListQueryHandler(
    IIfoodTokenProvider tokenProvider,
    IIfoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodMerchantsListQuery, IReadOnlyCollection<IfoodMerchantSummaryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodMerchantSummaryResponse>>> Handle(
        GetIfoodMerchantsListQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodMerchantsListQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var token = await tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IReadOnlyCollection<IfoodMerchantSummaryResponse>>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var result = await merchantClient.ListMerchantsAsync(token, request.Page, request.Size, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IfoodMerchantSummaryResponse>>(new Error("IfoodMerchant.ListFailed", result.ErrorMessage ?? "Falha ao listar as lojas no Ifood."));

                IReadOnlyCollection<IfoodMerchantSummaryResponse> merchants = result.Merchants
                    .Select(m => new IfoodMerchantSummaryResponse(m.Id, m.Name, m.CorporateName))
                    .ToList();

                return Result.Success(merchants);
            });
    }
}
