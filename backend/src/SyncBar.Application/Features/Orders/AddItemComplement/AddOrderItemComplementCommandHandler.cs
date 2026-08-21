using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.AddItemComplement;

internal sealed class AddOrderItemComplementCommandHandler : BaseCommandHandler<AddOrderItemComplementCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemComplementCommandHandler(
        ICustomerOrderRepository orderRepository,
        IComplementGroupRepository complementGroupRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _complementGroupRepository = complementGroupRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(AddOrderItemComplementCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddOrderItemComplementCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                userIdBox.Value = request.EmployeeId;

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId && i.IsActive);
                if (item is null)
                    return Result.Failure(new Error("CustomerOrder.ItemNotFound", "Order item not found."));

                var links = await _productComplementGroupRepository.GetByProductAsync(item.ProductId, cancellationToken);
                if (links.All(l => l.ComplementGroupId != request.ComplementGroupId))
                    return Result.Failure(new Error("OrderItem.ComplementGroupNotAvailable",
                        "This complement group is not available for the item's product."));

                var group = await _complementGroupRepository.GetByIdAsync(request.ComplementGroupId, cancellationToken);
                if (group is null || !group.IsActive)
                    return Result.Failure(new Error("ComplementGroup.NotFound", "Complement group not found."));

                var complement = group.Complements.FirstOrDefault(c => c.Id == request.ComplementId && c.IsActive);
                if (complement is null)
                    return Result.Failure(new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var result = order.AddComplement(request.OrderItemId, complement.Id, complement.ExtraPrice, currentTime);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
