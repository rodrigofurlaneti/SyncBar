using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class AcceptDeliveryAddressChangeCommandHandler : BaseCommandHandler<AcceptDeliveryAddressChangeCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodShippingClient _shippingClient;

    public AcceptDeliveryAddressChangeCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodShippingClient shippingClient,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _ifoodOrderRepository = ifoodOrderRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _shippingClient = shippingClient;
    }

    public override async Task<Result> Handle(AcceptDeliveryAddressChangeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AcceptDeliveryAddressChangeCommandHandler),
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

                var result = await _shippingClient.AcceptDeliveryAddressChangeAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodShipping.AcceptAddressChangeFailed", result.ErrorMessage ?? "Falha ao aceitar a troca de endereço no iFood."));

                return Result.Success();
            });
    }
}
