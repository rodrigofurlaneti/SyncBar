using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class CancelIFoodShippingDeliveryCommandHandler : BaseCommandHandler<CancelIFoodShippingDeliveryCommand>
{
    private readonly IIFoodShippingDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodShippingClient _shippingClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public CancelIFoodShippingDeliveryCommandHandler(
        IIFoodShippingDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodShippingClient shippingClient,
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

    public override async Task<Result> Handle(CancelIFoodShippingDeliveryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelIFoodShippingDeliveryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var delivery = await _deliveryRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
                if (delivery is null)
                    return Result.Failure(new Error("IFoodShippingDelivery.NotFound", "Entrega não encontrada."));

                var branch = await _branchRepository.GetByIdAsync(delivery.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var actionResult = await _shippingClient.CancelAsync(token, delivery.IFoodDeliveryId, request.Reason, request.CancellationCode, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFoodShipping.CancelFailed", actionResult.ErrorMessage ?? "Falha ao cancelar a entrega no iFood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var cancelResult = delivery.MarkCancelled(request.Reason, now);
                if (cancelResult.IsFailure)
                    return Result.Failure(cancelResult.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
