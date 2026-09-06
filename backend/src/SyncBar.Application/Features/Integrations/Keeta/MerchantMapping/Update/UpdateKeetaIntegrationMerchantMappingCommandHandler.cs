using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Update
{
    internal sealed class UpdateKeetaIntegrationMerchantMappingCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationMerchantMappingCommand>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationMerchantMappingCommandHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationMerchantMappingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationMerchantMappingCommandHandler),
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

                    if (request.IsAuthorized.HasValue || request.IsOnboarded.HasValue)
                    {
                        mapping.UpdateStatus(
                            request.IsAuthorized ?? mapping.IsAuthorized,
                            request.IsOnboarded ?? mapping.IsOnboarded);
                    }

                    if (request.MenuBaseUrl is not null || request.WebhookUrl is not null)
                    {
                        mapping.UpdateUrls(
                            request.MenuBaseUrl ?? mapping.MenuBaseUrl ?? string.Empty,
                            request.WebhookUrl ?? mapping.WebhookUrl ?? string.Empty);
                    }

                    if (request.RegisterMenuSync)
                    {
                        mapping.RegisterMenuSync();
                    }

                    _mappingRepository.Update(mapping);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
