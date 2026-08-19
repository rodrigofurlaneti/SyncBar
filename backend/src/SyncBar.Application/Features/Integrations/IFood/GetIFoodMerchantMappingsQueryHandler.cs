using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood;

internal sealed class GetIFoodMerchantMappingsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantMappingsQuery, IReadOnlyCollection<IFoodMerchantMappingResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodMerchantMappingResponse>>> Handle(
        GetIFoodMerchantMappingsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantMappingsQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var branches = await branchRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var mappings = await mappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                // Uma linha por filial ativa — inclusive as que ainda não têm MerchantId
                // configurado, pra tela deixar isso visível em vez de esconder.
                IReadOnlyCollection<IFoodMerchantMappingResponse> response = branches
                    .Where(b => b.IsActive)
                    .Select(b => mappings.TryGetValue(b.Id, out var mapping)
                        ? new IFoodMerchantMappingResponse(b.Id, b.Name, mapping.MerchantId, mapping.MerchantUuid)
                        : new IFoodMerchantMappingResponse(b.Id, b.Name, null, null))
                    .ToList();

                return Result.Success(response);
            });
    }
}
