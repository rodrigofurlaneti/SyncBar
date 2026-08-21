using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class MarkIFoodOrderReadyCommandHandler : BaseCommandHandler<MarkIFoodOrderReadyCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodOrderClient _orderClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public MarkIFoodOrderReadyCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodOrderClient orderClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _ifoodOrderRepository = ifoodOrderRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _orderClient = orderClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(MarkIFoodOrderReadyCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(MarkIFoodOrderReadyCommandHandler),
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

                var actionResult = await _orderClient.ReadyToPickupAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao marcar como pronto no iFood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                ifoodOrder.SetStatus(IFoodOrderStatuses.ReadyToPickup, now);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}
