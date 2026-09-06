using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Delete
{
    internal sealed class DeleteKeetaIntegrationAuthorizationSessionCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationAuthorizationSessionCommand>
    {
        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationAuthorizationSessionCommandHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationAuthorizationSessionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationAuthorizationSessionCommandHandler),
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

                    _sessionRepository.Delete(session);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
