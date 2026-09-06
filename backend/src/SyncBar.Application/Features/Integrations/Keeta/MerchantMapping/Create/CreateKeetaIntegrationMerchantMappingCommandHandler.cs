using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create
{
    internal sealed class CreateKeetaIntegrationMerchantMappingCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationMerchantMappingCommand, CreateKeetaIntegrationMerchantMappingResponse>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationMerchantMappingCommandHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationMerchantMappingResponse>> Handle(
            CreateKeetaIntegrationMerchantMappingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationMerchantMappingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _mappingRepository.ExistsByKeetaMerchantIdAsync(
                        request.KeetaMerchantId, cancellationToken);

                    if (exists)
                    {
                        return Result.Failure<CreateKeetaIntegrationMerchantMappingResponse>(
                            Error.Conflict(
                                "KeetaMerchantMapping.AlreadyExists",
                                $"Já existe um vínculo de merchant Keeta cadastrado para o ID {request.KeetaMerchantId}."));
                    }

                    var mappingResult = KeetaIntegrationMerchantMapping.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.InternalMerchantId,
                        request.KeetaMerchantId,
                        request.StoreName);

                    if (mappingResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationMerchantMappingResponse>(mappingResult.Error);

                    var mapping = mappingResult.Value;

                    await _mappingRepository.AddAsync(mapping, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationMerchantMappingResponse(
                        mapping.Id,
                        mapping.CompanyId,
                        mapping.BranchId,
                        mapping.InternalMerchantId,
                        mapping.KeetaMerchantId,
                        mapping.StoreName);

                    return Result.Success(response);
                });
        }
    }
}
