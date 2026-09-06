using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId
{
    internal sealed class ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandler
        : BaseQueryHandler<ExistsKeetaMerchantMappingByKeetaMerchantIdQuery, bool>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;

        public ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsKeetaMerchantMappingByKeetaMerchantIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _mappingRepository.ExistsByKeetaMerchantIdAsync(request.KeetaMerchantId, cancellationToken);
                    return Result.Success(exists);
                });
        }
    }
}
