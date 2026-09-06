using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById
{
    internal sealed class GetKeetaMerchantMappingByIdQueryHandler
        : BaseQueryHandler<GetKeetaMerchantMappingByIdQuery, KeetaIntegrationMerchantMappingResponse>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;

        public GetKeetaMerchantMappingByIdQueryHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
        }

        public override async Task<Result<KeetaIntegrationMerchantMappingResponse>> Handle(
            GetKeetaMerchantMappingByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaMerchantMappingByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var mapping = await _mappingRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (mapping is null)
                    {
                        return Result.Failure<KeetaIntegrationMerchantMappingResponse>(
                            Error.NotFound(
                                "KeetaMerchantMapping.NotFound",
                                $"Vínculo de merchant Keeta com ID {request.Id} não foi encontrado."));
                    }

                    var response = new KeetaIntegrationMerchantMappingResponse(
                        mapping.Id,
                        mapping.CompanyId,
                        mapping.BranchId,
                        mapping.InternalMerchantId,
                        mapping.KeetaMerchantId,
                        mapping.StoreName,
                        mapping.TimeZone,
                        mapping.IsAuthorized,
                        mapping.IsOnboarded,
                        mapping.LastMenuSyncAtUtc,
                        mapping.MenuBaseUrl,
                        mapping.WebhookUrl,
                        mapping.CreatedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
