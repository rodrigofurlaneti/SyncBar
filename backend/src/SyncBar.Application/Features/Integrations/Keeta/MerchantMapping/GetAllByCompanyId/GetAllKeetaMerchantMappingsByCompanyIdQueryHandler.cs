using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByCompanyId
{
    internal sealed class GetAllKeetaMerchantMappingsByCompanyIdQueryHandler
        : BaseQueryHandler<GetAllKeetaMerchantMappingsByCompanyIdQuery, IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;

        public GetAllKeetaMerchantMappingsByCompanyIdQueryHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
        }

        public override async Task<Result<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>> Handle(
            GetAllKeetaMerchantMappingsByCompanyIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllKeetaMerchantMappingsByCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var mappings = await _mappingRepository.GetAllByCompanyIdAsync(request.CompanyId, cancellationToken);

                    var response = mappings
                        .Select(m => new KeetaIntegrationMerchantMappingResponse(
                            m.Id,
                            m.CompanyId,
                            m.BranchId,
                            m.InternalMerchantId,
                            m.KeetaMerchantId,
                            m.StoreName,
                            m.TimeZone,
                            m.IsAuthorized,
                            m.IsOnboarded,
                            m.LastMenuSyncAtUtc,
                            m.MenuBaseUrl,
                            m.WebhookUrl,
                            m.CreatedAtUtc))
                        .ToList();

                    return Result.Success<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>(response);
                });
        }
    }
}
