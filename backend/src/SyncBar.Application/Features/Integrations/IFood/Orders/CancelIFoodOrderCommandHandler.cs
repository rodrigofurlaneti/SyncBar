using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Cancelamento no Ifood é assíncrono: aqui só ENVIAMOS o pedido de cancelamento
// (requestCancellation, 202 Accepted). O resultado real (evento CANCELLED ou
// CANCELLATION_REQUEST_FAILED) chega no próximo ciclo de polling — CANCELLED já é tratado em
// SyncIfoodOrdersCommandHandler (cancela o CustomerOrder vinculado). CANCELLATION_REQUEST_FAILED
// ainda não tem tratamento dedicado nesta fase (fica só logado/reconhecido) — ver
// Ifood-integration-status no projeto claude.ai.
internal sealed class CancelIfoodOrderCommandHandler : BaseCommandHandler<CancelIfoodOrderCommand>
{
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodOrderClient _orderClient;

    // IUnitOfWork não vira campo: só é repassado ao BaseCommandHandler (necessário para persistir
    // o LogTracker do ExecuteWithLogAsync). Este handler só DISPARA o pedido de cancelamento
    // assíncrono no Ifood (202 Accepted) e não altera nenhum estado local aqui — ver comentário
    // no topo do arquivo. Mesmo padrão de CancelIfoodOrderDriverRequestCommandHandler.
    public CancelIfoodOrderCommandHandler(
        IIfoodOrderRepository IfoodOrderRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodOrderClient orderClient,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _IfoodOrderRepository = IfoodOrderRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _orderClient = orderClient;
    }

    public override async Task<Result> Handle(CancelIfoodOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelIfoodOrderCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await _IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await _branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var actionResult = await _orderClient.RequestCancellationAsync(
                    token, IfoodOrder.IfoodOrderId, request.ReasonCode, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("Ifood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao solicitar cancelamento no Ifood."));

                return Result.Success();
            });
    }
}
