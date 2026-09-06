using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete
{
    internal sealed class DeleteKeetaIntegrationMerchantMappingCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationMerchantMappingCommand>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationMerchantMappingCommandHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationMerchantMappingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationMerchantMappingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var mapping = await _mappingRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (mapping is null || mapping.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaMerchantMapping.NotFound",
                                $"Vínculo de merchant Keeta com ID {request.Id} não foi encontrado para esta empresa."));
                    }

                    _mappingRepository.Delete(mapping);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
