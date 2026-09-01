using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class CancelIfoodShippingDeliveryCommandHandler : BaseCommandHandler<CancelIfoodShippingDeliveryCommand>
{
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodShippingClient _shippingClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public CancelIfoodShippingDeliveryCommandHandler(
        IIfoodShippingDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodShippingClient shippingClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _deliveryRepository = deliveryRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _shippingClient = shippingClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(CancelIfoodShippingDeliveryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelIfoodShippingDeliveryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var delivery = await _deliveryRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
                if (delivery is null)
                    return Result.Failure(new Error("IfoodShippingDelivery.NotFound", "Entrega não encontrada."));

                var branch = await _branchRepository.GetByIdAsync(delivery.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var actionResult = await _shippingClient.CancelAsync(token, delivery.IfoodDeliveryId, request.Reason, request.CancellationCode, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IfoodShipping.CancelFailed", actionResult.ErrorMessage ?? "Falha ao cancelar a entrega no Ifood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var cancelResult = delivery.MarkCancelled(request.Reason, now);
                if (cancelResult.IsFailure)
                    return Result.Failure(cancelResult.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
