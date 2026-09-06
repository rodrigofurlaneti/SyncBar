using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback
{
    internal sealed class HandleKeetaOAuthCallbackCommandHandler
        : BaseCommandHandler<HandleKeetaOAuthCallbackCommand, HandleKeetaOAuthCallbackResponse>
    {
        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IKeetaAuthClient _authClient;
        private readonly IKeetaAccessTokenProvider _tokenProvider;
        private readonly IUnitOfWork _unitOfWork;

        public HandleKeetaOAuthCallbackCommandHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            IKeetaAuthClient authClient,
            IKeetaAccessTokenProvider tokenProvider,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _mappingRepository = mappingRepository;
            _authClient = authClient;
            _tokenProvider = tokenProvider;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<HandleKeetaOAuthCallbackResponse>> Handle(
            HandleKeetaOAuthCallbackCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(HandleKeetaOAuthCallbackCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    // 1. Garante a sessão de autorização (idempotente — o navegador pode reenviar
                    // o callback em caso de retry/duplo clique do lojista).
                    var session = await _sessionRepository.GetByAuthIdAsync(request.AuthId, cancellationToken);

                    if (session is null)
                    {
                        var sessionResult = KeetaIntegrationAuthorizationSession.Create(
                            request.CompanyId, request.BranchId, request.AuthId, operationType: 1);

                        if (sessionResult.IsFailure)
                            return Result.Failure<HandleKeetaOAuthCallbackResponse>(sessionResult.Error);

                        session = sessionResult.Value;
                        session.SetDetails(request.KeetaMerchantId, request.Code, request.State);
                        await _sessionRepository.AddAsync(session, cancellationToken);
                    }
                    else
                    {
                        session.SetDetails(
                            request.KeetaMerchantId ?? session.KeetaMerchantId,
                            request.Code ?? session.AuthorizationCode,
                            request.State ?? session.State);
                        _sessionRepository.Update(session);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    // 2. A partir daqui buscamos ativamente as lojas autorizadas na Keeta — o modo
                    // "app_level_token" usado por esta integração não devolve o keetaMerchantId no
                    // próprio callback (isso só acontece no modo shop_level_authorization_code), e
                    // o webhook assíncrono de autorização pode demorar ou nem chegar a tempo do
                    // lojista ver a tela de sucesso. Consultar GET .../merchantInfo aqui mesmo
                    // torna o fluxo síncrono e independente do webhook.
                    var mappedIds = new List<long>();

                    try
                    {
                        var tokenResult = await _tokenProvider.GetValidAccessTokenAsync(
                            request.CompanyId, request.BranchId, cancellationToken: cancellationToken);
                        if (tokenResult.IsSuccess)
                        {
                            var merchantInfo = await _authClient.GetMerchantInfoAsync(
                                request.CompanyId, request.BranchId, request.AuthId, tokenResult.Value, cancellationToken: cancellationToken);

                            foreach (var shop in merchantInfo.AuthorizedShops)
                            {
                                var mapping = await _mappingRepository.GetByKeetaMerchantIdAsync(shop.Id, cancellationToken);

                                if (mapping is null)
                                {
                                    var mappingResult = KeetaIntegrationMerchantMapping.Create(
                                        request.CompanyId, request.BranchId, shop.Id.ToString(), shop.Id, shop.Name);

                                    if (mappingResult.IsSuccess)
                                    {
                                        await _mappingRepository.AddAsync(mappingResult.Value, cancellationToken);
                                        mappedIds.Add(shop.Id);
                                    }
                                }
                                else
                                {
                                    mapping.UpdateStatus(isAuthorized: true, isOnboarded: mapping.IsOnboarded);
                                    _mappingRepository.Update(mapping);
                                    mappedIds.Add(shop.Id);
                                }
                            }

                            session.MarkAsProcessed();
                            _sessionRepository.Update(session);
                            await _unitOfWork.CommitAsync(cancellationToken);
                        }
                    }
                    catch (Exception)
                    {
                        // A sessão já está persistida (passo 1) — se a consulta de merchantInfo
                        // falhar (rede/Keeta indisponível), a autorização em si não é perdida: o
                        // webhook assíncrono da Keeta (opType=1) ainda pode chegar e reconciliar, e
                        // este callback é reprocessável (idempotente) caso o lojista atualize a
                        // página. Não deixamos essa falha derrubar o redirecionamento do lojista.
                    }

                    return Result.Success(new HandleKeetaOAuthCallbackResponse(
                        session.Id, session.AuthId, session.IsProcessed, mappedIds));
                });
        }
    }
}
