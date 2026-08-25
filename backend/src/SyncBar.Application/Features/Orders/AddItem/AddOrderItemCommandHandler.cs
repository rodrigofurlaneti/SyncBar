using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.AddItem;

internal sealed class AddOrderItemCommandHandler : BaseCommandHandler<AddOrderItemCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IProductStockRepository _stockRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IPrintingService _printingService;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemCommandHandler(
        ICustomerOrderRepository orderRepository,
        IProductRepository productRepository,
        IPromotionRepository promotionRepository,
        IProductStockRepository stockRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        IComplementGroupRepository complementGroupRepository,
        IComplementItemRepository complementItemRepository,
        IPrintingService printingService,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
        _stockRepository = stockRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _complementGroupRepository = complementGroupRepository;
        _complementItemRepository = complementItemRepository;
        _printingService = printingService;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddOrderItemCommandHandler),
            nameof(Handle),
            null, 
            async (userIdBox) =>
            {
                userIdBox.Value = request.EmployeeId;
                return await HandleCoreAsync(request, cancellationToken);
            });
    }

    private async Task<Result> HandleCoreAsync(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        // 1. Busca e valida o pedido sequencialmente
        var orderResult = await GetOpenOrderAsync(request.CustomerOrderId, cancellationToken);
        if (orderResult.IsFailure)
            return Result.Failure(orderResult.Error);
        var order = orderResult.Value;

        // 2. Busca e valida o produto sequencialmente
        var productResult = await GetActiveProductAsync(request.ProductId, cancellationToken);
        if (productResult.IsFailure)
            return Result.Failure(productResult.Error);
        var product = productResult.Value;

        // 3. Busca as promoções sequencialmente
        var promotions = await _promotionRepository.GetByBranchAsync(order.BranchId, cancellationToken);

        // 4. Busca o estoque sequencialmente
        var stockSnapshot = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

        // 5. Se houver complementos selecionados, valida contra os grupos vinculados ao
        // produto ANTES de lançar o item — evita lançar o item e falhar depois no meio do caminho.
        var complementsResult = await ResolveComplementsAsync(product.Id, request, cancellationToken);
        if (complementsResult.IsFailure)
            return Result.Failure(complementsResult.Error);
        var resolvedComplements = complementsResult.Value;

        var itemCountBefore = order.Items.Count;
        // O domínio usa DateTime puro (DATETIME2 no banco), não DateTimeOffset —
        // e o padrão do projeto é hora LOCAL (ver OpenOrderCommandHandler), não UTC,
        // já que o front-end interpreta as datas recebidas como hora local sem conversão.
        var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;
        var activePromotion = FindActivePromotion(promotions, product.Id, currentTime);

        // Cria o OrderItem via factory do agregado CustomerOrder (Quantity > 0 e UnitPrice
        // congelado no lançamento são garantidos dentro do próprio domínio).
        var addItemResult = AddPrimaryItem(order, product, request, activePromotion, currentTime);
        if (addItemResult.IsFailure)
            return addItemResult;

        // 6. Aplica os complementos resolvidos na linha PRINCIPAL recém-lançada (a primeira
        // adicionada por AddItemWithPromotion — o item bônus de promoção EmDobro, se houver,
        // não recebe complementos).
        var applyComplementsResult = ApplyComplementsToOrder(order, itemCountBefore, resolvedComplements, currentTime);
        if (applyComplementsResult.IsFailure)
            return applyComplementsResult;

        // Baixa o estoque do próprio produto lançado.
        var primaryStockResult = DeductPrimaryStock(order, stockSnapshot, itemCountBefore, request.EmployeeId, currentTime);
        if (primaryStockResult.IsFailure)
            return primaryStockResult;

        // 7. Fase 18 (combos) — complementos cujo ComplementItem aponta pra um Product real
        // (LinkedProductId) também baixam o estoque DAQUELE produto, na mesma quantidade do
        // item principal (ex.: 2 combos = baixa 2 unidades do sanduíche vinculado à opção
        // escolhida). Complementos sem LinkedProductId (a maioria — "sem cebola", "bacon
        // extra") não têm produto próprio, então não geram movimentação aqui.
        var linkedStockResult = await DeductLinkedComplementStockAsync(
            order, itemCountBefore, resolvedComplements, request.EmployeeId, currentTime, cancellationToken);
        if (linkedStockResult.IsFailure)
            return linkedStockResult;

        // Persiste tudo em uma única transação (item, complementos e movimentações de estoque).
        var commitResult = await CommitOrderAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult;

        TriggerPrinting(order, itemCountBefore);

        return Result.Success();
    }

    private async Task<Result<CustomerOrder>> GetOpenOrderAsync(long customerOrderId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(customerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<CustomerOrder>(new Error("CustomerOrder.NotFound", "Order not found."));

        return Result.Success(order);
    }

    private async Task<Result<Product>> GetActiveProductAsync(long productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<Product>(new Error("Product.NotFound", "Product not found."));

        return Result.Success(product);
    }

    private async Task<Result<List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>>> ResolveComplementsAsync(
        long productId, AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var resolvedComplements = new List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>();
        if (request.Complements is not { Count: > 0 })
            return Result.Success(resolvedComplements);

        var links = await _productComplementGroupRepository.GetByProductAsync(productId, cancellationToken);
        var allowedGroupIds = links.Select(l => l.ComplementGroupId).ToHashSet();

        foreach (var selection in request.Complements)
        {
            if (!allowedGroupIds.Contains(selection.ComplementGroupId))
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>>(
                    new Error("OrderItem.ComplementGroupNotAvailable",
                        $"Complement group {selection.ComplementGroupId} is not available for this product."));

            var group = await _complementGroupRepository.GetByIdAsync(selection.ComplementGroupId, cancellationToken);
            if (group is null || !group.IsActive)
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>>(
                    new Error("ComplementGroup.NotFound", "Complement group not found."));

            var complement = group.Complements.FirstOrDefault(c => c.Id == selection.ComplementId && c.IsActive);
            if (complement is null)
                return Result.Failure<List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>>(
                    new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

            resolvedComplements.Add((complement.Id, complement.ExtraPrice, complement.ComplementItemId));
        }

        return Result.Success(resolvedComplements);
    }

    private static Promotion? FindActivePromotion(IEnumerable<Promotion> promotions, long productId, DateTime currentTime)
        => promotions.FirstOrDefault(promo => promo.ProductId == productId && promo.IsActiveAt(currentTime));

    private static Result AddPrimaryItem(
        CustomerOrder order, Product product, AddOrderItemCommand request, Promotion? activePromotion, DateTime currentTime)
        => order.AddItemWithPromotion(product, request.Quantity, request.Notes, activePromotion, request.EmployeeId ?? 0, currentTime);

    private static Result ApplyComplementsToOrder(
        CustomerOrder order,
        int itemCountBefore,
        List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)> resolvedComplements,
        DateTime currentTime)
    {
        if (resolvedComplements.Count == 0)
            return Result.Success();

        var primaryItemId = order.Items.ElementAt(itemCountBefore).Id;
        foreach (var (complementId, extraPrice, _) in resolvedComplements)
        {
            var complementResult = order.AddComplement(primaryItemId, complementId, extraPrice, currentTime);
            if (complementResult.IsFailure)
                return complementResult;
        }

        return Result.Success();
    }

    private Result DeductPrimaryStock(
        CustomerOrder order, ProductStock? stockSnapshot, int itemCountBefore, long? employeeId, DateTime currentTime)
    {
        if (stockSnapshot is null)
            return Result.Success();

        var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

        var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
        if (stockResult.IsFailure)
            return Result.Failure(stockResult.Error);

        long? movementEmployeeId = employeeId != 0 ? employeeId : null;

        var movementResult = StockMovement.Create(
            stockItemId: stockSnapshot.ProductId,
            stockMovementTypeId: 2, // Tipo: Venda/Saída
            purchaseItemId: null,
            orderItemId: order.Items.Last().Id,
            employeeId: movementEmployeeId,
            quantity: -totalQuantityAdded,
            unitCost: null,
            totalCost: null,
            documentNumber: null,
            movedAt: currentTime,
            notes: $"Baixa automática do pedido {order.Id}"
        );

        if (movementResult.IsFailure)
            return Result.Failure(movementResult.Error);

        _stockRepository.AddMovement(movementResult.Value);
        return Result.Success();
    }

    private async Task<Result> DeductLinkedComplementStockAsync(
        CustomerOrder order,
        int itemCountBefore,
        List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)> resolvedComplements,
        long? employeeId,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        if (resolvedComplements.Count == 0)
            return Result.Success();

        // Só a linha principal recebe complementos (o item bônus da promoção EmDobro
        // não recebe), então a baixa do produto vinculado segue a quantidade dessa
        // linha — nunca a soma de todas as linhas recém-lançadas.
        var primaryItem = order.Items.ElementAt(itemCountBefore);
        var primaryQuantity = primaryItem.Quantity;
        var complementItemIds = resolvedComplements.Select(c => c.ComplementItemId).Distinct().ToList();
        var complementItems = await _complementItemRepository.GetByIdsAsync(complementItemIds, cancellationToken);
        var linkedProductIdsByComplementItemId = complementItems
            .Where(ci => ci.LinkedProductId.HasValue)
            .ToDictionary(ci => ci.Id, ci => ci.LinkedProductId!.Value);

        // O repositório devolve um snapshot novo a cada consulta, então dois
        // complementos que apontam pro MESMO produto vinculado precisam compartilhar
        // a mesma instância — senão cada um deduz sobre o saldo original e o estoque
        // pode ficar negativo sem falhar a checagem de suficiência.
        var linkedStocksByProductId = new Dictionary<long, ProductStock?>();

        foreach (var (complementId, _, complementItemId) in resolvedComplements)
        {
            var deductResult = await DeductLinkedComplementStockForOneAsync(
                order, primaryItem, primaryQuantity, complementId, complementItemId,
                linkedProductIdsByComplementItemId, linkedStocksByProductId, employeeId, currentTime, cancellationToken);

            if (deductResult.IsFailure)
                return deductResult;
        }

        return Result.Success();
    }

    private async Task<Result> DeductLinkedComplementStockForOneAsync(
        CustomerOrder order,
        OrderItem primaryItem,
        decimal primaryQuantity,
        long complementId,
        long complementItemId,
        IReadOnlyDictionary<long, long> linkedProductIdsByComplementItemId,
        Dictionary<long, ProductStock?> linkedStocksByProductId,
        long? employeeId,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        if (!linkedProductIdsByComplementItemId.TryGetValue(complementItemId, out var linkedProductId))
            return Result.Success();

        if (!linkedStocksByProductId.TryGetValue(linkedProductId, out var linkedStock))
        {
            linkedStock = await _stockRepository.GetByProductIdAsync(linkedProductId, cancellationToken);
            linkedStocksByProductId[linkedProductId] = linkedStock;
        }

        if (linkedStock is null)
            return Result.Success(); // Produto vinculado não é controlado por estoque — nada a baixar.

        var linkedStockResult = linkedStock.Deduct(primaryQuantity);
        if (linkedStockResult.IsFailure)
            return Result.Failure(linkedStockResult.Error);

        var linkedMovementEmployeeId = employeeId is > 0 ? employeeId : null;
        var linkedMovementResult = StockMovement.Create(
            stockItemId: linkedStock.ProductId,
            stockMovementTypeId: 2, // Tipo: Venda/Saída
            purchaseItemId: null,
            orderItemId: primaryItem.Id,
            employeeId: linkedMovementEmployeeId,
            quantity: -primaryQuantity,
            unitCost: null,
            totalCost: null,
            documentNumber: null,
            movedAt: currentTime,
            notes: $"Baixa automática do pedido {order.Id} (combo — complemento {complementId})"
        );

        if (linkedMovementResult.IsFailure)
            return Result.Failure(linkedMovementResult.Error);

        _stockRepository.AddMovement(linkedMovementResult.Value);
        return Result.Success();
    }

    private async Task<Result> CommitOrderAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(new Error("Stock.Concurrency",
                "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
        }
    }

    private void TriggerPrinting(CustomerOrder order, int itemCountBefore)
    {
        var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
        if (newItemIds.Any())
        {
            _ = _printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
        }
    }
}
