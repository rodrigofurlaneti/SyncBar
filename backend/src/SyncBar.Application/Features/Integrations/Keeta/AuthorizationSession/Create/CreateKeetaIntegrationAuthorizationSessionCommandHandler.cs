using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create
{
    internal sealed class CreateKeetaIntegrationAuthorizationSessionCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationAuthorizationSessionCommand, CreateKeetaIntegrationAuthorizationSessionResponse>
    {
        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationAuthorizationSessionCommandHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationAuthorizationSessionResponse>> Handle(
            CreateKeetaIntegrationAuthorizationSessionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationAuthorizationSessionCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var existing = await _sessionRepository.GetByAuthIdAsync(request.AuthId, cancellationToken);
                    if (existing is not null)
                    {
                        return Result.Failure<CreateKeetaIntegrationAuthorizationSessionResponse>(
                            Error.Conflict(
                                "KeetaAuthorizationSession.AlreadyExists",
                                $"Já existe uma sessão de autorização Keeta cadastrada para o AuthId {request.AuthId}."));
                    }

                    var sessionResult = KeetaIntegrationAuthorizationSession.Create(
                        request.CompanyId, request.BranchId, request.AuthId, request.OperationType);

                    if (sessionResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationAuthorizationSessionResponse>(sessionResult.Error);

                    var session = sessionResult.Value;

                    await _sessionRepository.AddAsync(session, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationAuthorizationSessionResponse(
                        session.Id, session.CompanyId, session.BranchId, session.AuthId, session.OperationType);

                    return Result.Success(response);
                });
        }
    }
}
