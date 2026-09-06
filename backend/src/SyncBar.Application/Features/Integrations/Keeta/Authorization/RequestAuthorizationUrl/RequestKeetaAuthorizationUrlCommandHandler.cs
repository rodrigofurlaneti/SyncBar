using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl
{
    internal sealed class RequestKeetaAuthorizationUrlCommandHandler
        : BaseCommandHandler<RequestKeetaAuthorizationUrlCommand, RequestKeetaAuthorizationUrlResponse>
    {
        private readonly IKeetaAuthClient _authClient;

        public RequestKeetaAuthorizationUrlCommandHandler(
            IKeetaAuthClient authClient,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _authClient = authClient;
        }

        public override async Task<Result<RequestKeetaAuthorizationUrlResponse>> Handle(
            RequestKeetaAuthorizationUrlCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RequestKeetaAuthorizationUrlCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    // O único jeito de correlacionar a autorização assíncrona (webhook) e o
                    // callback do navegador com a empresa/filial que a iniciou é embutir esses
                    // dados no próprio redirectUri que a Keeta vai devolver intacto ao navegador
                    // ao final da autorização — a API de autorização não aceita nenhum outro
                    // parâmetro de correlação (não existe "state" no request desse endpoint).
                    var separator = request.RedirectUri.Contains('?') ? '&' : '?';
                    var correlatedRedirectUri =
                        $"{request.RedirectUri}{separator}companyId={request.CompanyId}&branchId={request.BranchId}";

                    var url = await _authClient.GetAuthorizationUrlAsync(
                        request.CompanyId,
                        request.BranchId,
                        correlatedRedirectUri,
                        cancellationToken);

                    return Result.Success(new RequestKeetaAuthorizationUrlResponse(url));
                });
        }
    }
}
