using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Cancelamento no iFood é assíncrono: aqui só ENVIAMOS o pedido de cancelamento
// (requestCancellation, 202 Accepted). O resultado real (evento CANCELLED ou
// CANCELLATION_REQUEST_FAILED) chega no próximo ciclo de polling — CANCELLED já é tratado em
// SyncIFoodOrdersCommandHandler (cancela o CustomerOrder vinculado). CANCELLATION_REQUEST_FAILED
// ainda não tem tratamento dedicado nesta fase (fica só logado/reconhecido) — ver
// ifood-integration-status no projeto claude.ai.
internal sealed class CancelIFoodOrderCommandHandler : BaseCommandHandler<CancelIFoodOrderCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodOrderClient _orderClient;
    private readonly IUnitOfWork _unitOfWork;

    public CancelIFoodOrderCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodOrderClient orderClient,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _ifoodOrderRepository = ifoodOrderRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _orderClient = orderClient;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(CancelIFoodOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelIFoodOrderCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await _ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await _branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var actionResult = await _orderClient.RequestCancellationAsync(
                    token, ifoodOrder.IFoodOrderId, request.ReasonCode, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao solicitar cancelamento no iFood."));

                return Result.Success();
            });
    }
}
