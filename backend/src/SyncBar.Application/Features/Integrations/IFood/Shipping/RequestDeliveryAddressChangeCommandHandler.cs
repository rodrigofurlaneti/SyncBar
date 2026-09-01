using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class RequestDeliveryAddressChangeCommandHandler : BaseCommandHandler<RequestDeliveryAddressChangeCommand>
{
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodShippingClient _shippingClient;

    public RequestDeliveryAddressChangeCommandHandler(
        IIfoodOrderRepository IfoodOrderRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodShippingClient shippingClient,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _IfoodOrderRepository = IfoodOrderRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _shippingClient = shippingClient;
    }

    public override async Task<Result> Handle(RequestDeliveryAddressChangeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestDeliveryAddressChangeCommandHandler),
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

                var payload = new IfoodShippingDeliveryAddressChangePayload(
                    request.StreetNumber, request.StreetName, request.Complement, request.Neighborhood,
                    request.City, request.State, string.IsNullOrWhiteSpace(request.Country) ? "BR" : request.Country,
                    request.Reference, request.Latitude, request.Longitude);

                var result = await _shippingClient.RequestDeliveryAddressChangeAsync(token, IfoodOrder.IfoodOrderId, payload, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodShipping.RequestAddressChangeFailed", result.ErrorMessage ?? "Falha ao solicitar a troca de endereço no Ifood."));

                return Result.Success();
            });
    }
}
