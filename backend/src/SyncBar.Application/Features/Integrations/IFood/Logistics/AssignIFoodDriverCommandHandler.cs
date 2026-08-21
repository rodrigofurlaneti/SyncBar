using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

internal sealed class AssignIFoodDriverCommandHandler : BaseCommandHandler<AssignIFoodDriverCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IIFoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodLogisticsClient _logisticsClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AssignIFoodDriverCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IIFoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodLogisticsClient logisticsClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _ifoodOrderRepository = ifoodOrderRepository;
        _deliveryRepository = deliveryRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _logisticsClient = logisticsClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(AssignIFoodDriverCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AssignIFoodDriverCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await _ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var existing = await _deliveryRepository.GetByIFoodOrderIdAsync(ifoodOrder.Id, cancellationToken);
                if (existing is not null)
                    return Result.Failure(new Error("IFoodLogisticsDelivery.AlreadyAssigned", "Este pedido já tem um entregador atribuído."));

                var branch = await _branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var actionResult = await _logisticsClient.AssignDriverAsync(
                    token, ifoodOrder.IFoodOrderId, request.DriverName, request.DriverPhone, request.DriverVehicleType, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao atribuir entregador no iFood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var deliveryResult = IFoodLogisticsDelivery.Create(
                    ifoodOrder.Id, ifoodOrder.BranchId, request.DriverName, request.DriverPhone, request.DriverVehicleType, now);
                if (deliveryResult.IsFailure)
                    return Result.Failure(deliveryResult.Error);

                await _deliveryRepository.AddAsync(deliveryResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}
