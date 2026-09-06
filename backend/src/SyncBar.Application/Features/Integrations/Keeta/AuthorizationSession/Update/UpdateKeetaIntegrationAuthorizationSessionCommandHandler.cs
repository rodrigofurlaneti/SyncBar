using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update
{
    internal sealed class UpdateKeetaIntegrationAuthorizationSessionCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationAuthorizationSessionCommand>
    {
        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationAuthorizationSessionCommandHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationAuthorizationSessionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationAuthorizationSessionCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var session = await _sessionRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (session is null || session.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaAuthorizationSession.NotFound",
                                $"Sessão de autorização Keeta com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    if (request.KeetaMerchantId.HasValue || request.AuthorizationCode is not null || request.State is not null)
                    {
                        session.SetDetails(
                            request.KeetaMerchantId ?? session.KeetaMerchantId,
                            request.AuthorizationCode ?? session.AuthorizationCode,
                            request.State ?? session.State);
                    }

                    if (request.MarkAsProcessed)
                    {
                        session.MarkAsProcessed();
                    }

                    _sessionRepository.Update(session);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
