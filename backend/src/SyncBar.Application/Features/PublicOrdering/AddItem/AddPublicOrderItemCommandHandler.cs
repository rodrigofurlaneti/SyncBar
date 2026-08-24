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
                var tableResult = await ValidateTableAsync(request.Token, cancellationToken);
                if (tableResult.IsFailure)
                    return Result.Failure<long>(tableResult.Error);
                var table = tableResult.Value;

                var branchResult = await ValidateBranchAsync(table.BranchId, cancellationToken);
                if (branchResult.IsFailure)
                    return Result.Failure<long>(branchResult.Error);
                var branch = branchResult.Value;

                // Associa a ação no log ao funcionário "virtual" de autoatendimento configurado na filial
                userIdBox.Value = branch.SelfServiceEmployeeId.Value;

                var productResult = await ValidateProductAsync(request.ProductId, branch.CompanyId, cancellationToken);
                if (productResult.IsFailure)
                    return Result.Failure<long>(productResult.Error);
                var product = productResult.Value;

                var complementsResult = await ResolveComplementsAsync(product, request, cancellationToken);
                if (complementsResult.IsFailure)
                    return Result.Failure<long>(complementsResult.Error);
                var resolvedComplements = complementsResult.Value;

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var orderResult = await GetOrCreateOrderAsync(table, branch, currentTime, cancellationToken);
                if (orderResult.IsFailure)
                    return Result.Failure<long>(orderResult.Error);
                var (order, isNewOrder) = orderResult.Value;

                if (isNewOrder)
                    table.ChangeStatus(TableStatusIds.Ocupada);

                var addItemResult = AddItemWithComplements(order, product, request, resolvedComplements, currentTime);
                if (addItemResult.IsFailure)
                    return Result.Failure<long>(addItemResult.Error);
                var itemCountBefore = addItemResult.Value;

                await _unitOfWork.CommitAsync(cancellationToken);

                var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
                await PrintNewItemsAsync(order.Id, newItemIds, cancellationToken);

                return Result.Success(order.Id);
            });
    }

    private async Task<Result<DiningTable>> ValidateTableAsync(Guid token, CancellationToken cancellationToken)
    {
        var table = await _diningTableRepository.GetByQrTokenAsync(token, cancellationToken);
        if (table is null || !table.IsActive)
            return Result.Failure<DiningTable>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

        return Result.Success(table);
    }

    private async Task<Result<Branch>> ValidateBranchAsync(long branchId, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null || !branch.IsActive)
            return Result.Failure<Branch>(new Error("Branch.NotFound", "Branch not found."));

        if (!branch.SelfServiceEmployeeId.HasValue)
            return Result.Failure<Branch>(new Error("Branch.SelfServiceDisabled",
                "Self-service ordering is not enabled for this branch. Ask the manager to configure it."));

        return Result.Success(branch);
    }

    private async Task<Result<Product>> ValidateProductAsync(long productId, long companyId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsActive || product.CompanyId != companyId)
            return Result.Failure<Product>(new Error("Product.NotFound", "Product not found."));

        return Result.Success(product);
    }

    // Mesma validação de AddOrderItemCommandHandler: resolve e valida os complementos
    // ANTES de tocar no pedido, pra não deixar o item lançado se um complemento for inválido.
    private async Task<Result<List<(long ComplementId, decimal ExtraPrice)>>> ResolveComplementsAsync(
        Product product, AddPublicOrderItemCommand request, CancellationToken cancellationToken)
    {
        var resolvedComplements = new List<(long ComplementId, decimal ExtraPrice)>();
        if (request.Complements is not { Count: > 0 })
            return Result.Success(resolvedComplements);

        var links = await _productComplementGroupRepository.GetByProductAsync(product.Id, cancellationToken);
        var allowedGroupIds = links.Select(l => l.ComplementGroupId).ToHashSet();

        foreach (var selection in request.Complements)
        {
            if (!allowedGroupIds.Contains(selection.ComplementGroupId))
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("OrderItem.ComplementGroupNotAvailable",
                    $"Complement group {selection.ComplementGroupId} is not available for this product."));

            var group = await _complementGroupRepository.GetByIdAsync(selection.ComplementGroupId, cancellationToken);
            if (group is null || !group.IsActive)
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("ComplementGroup.NotFound", "Complement group not found."));

            var complement = group.Complements.FirstOrDefault(c => c.Id == selection.ComplementId && c.IsActive);
            if (complement is null)
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

            resolvedComplements.Add((complement.Id, complement.ExtraPrice));
        }

        return Result.Success(resolvedComplements);
    }

    private async Task<Result<(CustomerOrder Order, bool IsNewOrder)>> GetOrCreateOrderAsync(
        DiningTable table, Branch branch, DateTime currentTime, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOpenByTableForUpdateAsync(table.Id, cancellationToken);
        if (order is not null)
            return Result.Success((order, false));

        // Passando o currentTime para o Create
        var created = CustomerOrder.Create(
            table.BranchId, table.Id, null, branch.SelfServiceEmployeeId.Value,
            null, "Pedido via QR Code", currentTime, null, OrderTypeIds.Mesa);

        if (created.IsFailure)
            return Result.Failure<(CustomerOrder, bool)>(created.Error);

        order = created.Value;
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success((order, true));
    }

    private static Result<int> AddItemWithComplements(
        CustomerOrder order,
        Product product,
        AddPublicOrderItemCommand request,
        List<(long ComplementId, decimal ExtraPrice)> resolvedComplements,
        DateTime currentTime)
    {
        var itemCountBefore = order.Items.Count;

        // Passando o currentTime para o AddItem
        var added = order.AddItem(product.Id, product.SalePrice, request.Quantity, request.Notes, null, currentTime);
        if (added.IsFailure)
            return Result.Failure<int>(added.Error);

        if (resolvedComplements.Count == 0)
            return Result.Success(itemCountBefore);

        var primaryItemId = order.Items.ElementAt(itemCountBefore).Id;
        foreach (var (complementId, extraPrice) in resolvedComplements)
        {
            var complementResult = order.AddComplement(primaryItemId, complementId, extraPrice, currentTime);
            if (complementResult.IsFailure)
                return Result.Failure<int>(complementResult.Error);
        }

        return Result.Success(itemCountBefore);
    }

    private async Task PrintNewItemsAsync(long orderId, List<long> newItemIds, CancellationToken cancellationToken)
    {
        try
        {
            await _printingService.PrintOrderItemsAsync(orderId, newItemIds, cancellationToken);
        }
        catch
        {
            // silencioso
        }
    }
}
