using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

internal sealed class AddPublicOrderItemCommandHandler : BaseCommandHandler<AddPublicOrderItemCommand, long>
{
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IPrintingService _printingService;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AddPublicOrderItemCommandHandler(
        IDiningTableRepository diningTableRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        ICustomerOrderRepository orderRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        IComplementGroupRepository complementGroupRepository,
        IPrintingService printingService,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _diningTableRepository = diningTableRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _complementGroupRepository = complementGroupRepository;
        _printingService = printingService;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(AddPublicOrderItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddPublicOrderItemCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP do cliente lido no request, caso aplicável
            async (userIdBox) =>
            {
                var table = await _diningTableRepository.GetByQrTokenAsync(request.Token, cancellationToken);
                if (table is null || !table.IsActive)
                    return Result.Failure<long>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

                var branch = await _branchRepository.GetByIdAsync(table.BranchId, cancellationToken);
                if (branch is null || !branch.IsActive)
                    return Result.Failure<long>(new Error("Branch.NotFound", "Branch not found."));
                if (!branch.SelfServiceEmployeeId.HasValue)
                    return Result.Failure<long>(new Error("Branch.SelfServiceDisabled",
                        "Self-service ordering is not enabled for this branch. Ask the manager to configure it."));

                // Associa a ação no log ao funcionário "virtual" de autoatendimento configurado na filial
                userIdBox.Value = branch.SelfServiceEmployeeId.Value;

                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive || product.CompanyId != branch.CompanyId)
                    return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

                // Mesma validação de AddOrderItemCommandHandler: resolve e valida os complementos
                // ANTES de tocar no pedido, pra não deixar o item lançado se um complemento for inválido.
                var resolvedComplements = new List<(long ComplementId, decimal ExtraPrice)>();
                if (request.Complements is { Count: > 0 })
                {
                    var links = await _productComplementGroupRepository.GetByProductAsync(product.Id, cancellationToken);
                    var allowedGroupIds = links.Select(l => l.ComplementGroupId).ToHashSet();

                    foreach (var selection in request.Complements)
                    {
                        if (!allowedGroupIds.Contains(selection.ComplementGroupId))
                            return Result.Failure<long>(new Error("OrderItem.ComplementGroupNotAvailable",
                                $"Complement group {selection.ComplementGroupId} is not available for this product."));

                        var group = await _complementGroupRepository.GetByIdAsync(selection.ComplementGroupId, cancellationToken);
                        if (group is null || !group.IsActive)
                            return Result.Failure<long>(new Error("ComplementGroup.NotFound", "Complement group not found."));

                        var complement = group.Complements.FirstOrDefault(c => c.Id == selection.ComplementId && c.IsActive);
                        if (complement is null)
                            return Result.Failure<long>(new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

                        resolvedComplements.Add((complement.Id, complement.ExtraPrice));
                    }
                }

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var order = await _orderRepository.GetOpenByTableForUpdateAsync(table.Id, cancellationToken);
                var isNewOrder = order is null;
                if (order is null)
                {
                    // Passando o currentTime para o Create
                    var created = CustomerOrder.Create(
                        table.BranchId, table.Id, null, branch.SelfServiceEmployeeId.Value,
                        null, "Pedido via QR Code", currentTime, null, OrderTypeIds.Mesa);

                    if (created.IsFailure)
                        return Result.Failure<long>(created.Error);

                    order = created.Value;
                    await _orderRepository.AddAsync(order, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                }

                if (isNewOrder)
                    table.ChangeStatus(TableStatusIds.Ocupada);

                var itemCountBefore = order.Items.Count;

                // Passando o currentTime para o AddItem
                var added = order.AddItem(product.Id, product.SalePrice, request.Quantity, request.Notes, null, currentTime);
                if (added.IsFailure)
                    return Result.Failure<long>(added.Error);

                if (resolvedComplements.Count > 0)
                {
                    var primaryItemId = order.Items.ElementAt(itemCountBefore).Id;
                    foreach (var (complementId, extraPrice) in resolvedComplements)
                    {
                        var complementResult = order.AddComplement(primaryItemId, complementId, extraPrice, currentTime);
                        if (complementResult.IsFailure)
                            return Result.Failure<long>(complementResult.Error);
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
                try
                {
                    await _printingService.PrintOrderItemsAsync(order.Id, newItemIds, cancellationToken);
                }
                catch
                {
                    // silencioso
                }

                return Result.Success(order.Id);
            });
    }
}
