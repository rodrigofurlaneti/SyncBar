using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById
{
    internal sealed class GetKeetaAuthorizationSessionByIdQueryHandler
        : BaseQueryHandler<GetKeetaAuthorizationSessionByIdQuery, KeetaIntegrationAuthorizationSessionResponse>
    {
        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;

        public GetKeetaAuthorizationSessionByIdQueryHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
        }

        public override async Task<Result<KeetaIntegrationAuthorizationSessionResponse>> Handle(
            GetKeetaAuthorizationSessionByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaAuthorizationSessionByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var session = await _sessionRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (session is null)
                    {
                        return Result.Failure<KeetaIntegrationAuthorizationSessionResponse>(
                            Error.NotFound(
                                "KeetaAuthorizationSession.NotFound",
                                $"Sessão de autorização Keeta com ID {request.Id} não foi encontrada."));
                    }

                    var response = new KeetaIntegrationAuthorizationSessionResponse(
                        session.Id,
                        session.CompanyId,
                        session.BranchId,
                        session.AuthId,
                        session.State,
                        session.KeetaMerchantId,
                        session.AuthorizationCode,
                        session.OperationType,
                        session.IsProcessed,
                        session.CreatedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
