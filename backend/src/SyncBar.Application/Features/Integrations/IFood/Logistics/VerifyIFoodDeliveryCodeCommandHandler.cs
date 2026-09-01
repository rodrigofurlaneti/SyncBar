using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

internal sealed class VerifyIfoodDeliveryCodeCommandHandler : BaseCommandHandler<VerifyIfoodDeliveryCodeCommand, bool>
{
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodLogisticsClient _logisticsClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyIfoodDeliveryCodeCommandHandler(
        IIfoodOrderRepository IfoodOrderRepository,
        IIfoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodLogisticsClient logisticsClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _IfoodOrderRepository = IfoodOrderRepository;
        _deliveryRepository = deliveryRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _logisticsClient = logisticsClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<bool>> Handle(VerifyIfoodDeliveryCodeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(VerifyIfoodDeliveryCodeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await _IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<bool>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var delivery = await _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(IfoodOrder.Id, cancellationToken);
                if (delivery is null)
                    return Result.Failure<bool>(new Error("IfoodLogisticsDelivery.NotFound", "Nenhum entregador atribuído a este pedido."));

                var branch = await _branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<bool>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<bool>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var verifyResult = await _logisticsClient.VerifyDeliveryCodeAsync(token, IfoodOrder.IfoodOrderId, request.Code, cancellationToken);
                if (!verifyResult.Success)
                    return Result.Failure<bool>(new Error("Ifood.ActionFailed", verifyResult.ErrorMessage ?? "Falha ao verificar o código de entrega no Ifood."));

                if (!verifyResult.CodeMatched)
                    return Result.Success(false); // código errado — não avança o estado local, entregador pode tentar de novo

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var transition = delivery.MarkDeliveryCodeVerified(now);
                if (transition.IsFailure)
                    return Result.Failure<bool>(transition.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(true);
            });
    }
}
