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
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Mapeia o ID do funcionário (usuário) responsável pela ação para o log de auditoria
                userIdBox.Value = request.EmployeeId;

                // 1. Busca o pedido sequencialmente
                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                // 2. Busca o produto sequencialmente
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));

                // 3. Busca as promoções sequencialmente
                var promotions = await _promotionRepository.GetByBranchAsync(order.BranchId, cancellationToken);

                // 4. Busca o estoque sequencialmente
                var stockSnapshot = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

                // 5. Se houver complementos selecionados, valida contra os grupos vinculados ao
                // produto ANTES de lançar o item — evita lançar o item e falhar depois no meio do caminho.
                var resolvedComplements = new List<(long ComplementId, decimal ExtraPrice, long ComplementItemId)>();
                if (request.Complements is { Count: > 0 })
                {
                    var links = await _productComplementGroupRepository.GetByProductAsync(product.Id, cancellationToken);
                    var allowedGroupIds = links.Select(l => l.ComplementGroupId).ToHashSet();

                    foreach (var selection in request.Complements)
                    {
                        if (!allowedGroupIds.Contains(selection.ComplementGroupId))
                            return Result.Failure(new Error("OrderItem.ComplementGroupNotAvailable",
                                $"Complement group {selection.ComplementGroupId} is not available for this product."));

                        var group = await _complementGroupRepository.GetByIdAsync(selection.ComplementGroupId, cancellationToken);
                        if (group is null || !group.IsActive)
                            return Result.Failure(new Error("ComplementGroup.NotFound", "Complement group not found."));

                        var complement = group.Complements.FirstOrDefault(c => c.Id == selection.ComplementId && c.IsActive);
                        if (complement is null)
                            return Result.Failure(new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

                        resolvedComplements.Add((complement.Id, complement.ExtraPrice, complement.ComplementItemId));
                    }
                }

                var itemCountBefore = order.Items.Count;
                // O domínio usa DateTime puro (DATETIME2 no banco), não DateTimeOffset —
                // e o padrão do projeto é hora LOCAL (ver OpenOrderCommandHandler), não UTC,
                // já que o front-end interpreta as datas recebidas como hora local sem conversão.
                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var activePromotion = promotions.FirstOrDefault(promo =>
                    promo.ProductId == product.Id && promo.IsActiveAt(currentTime));

                var result = order.AddItemWithPromotion(product, request.Quantity, request.Notes, activePromotion, request.EmployeeId ?? 0, currentTime);
                if (result.IsFailure)
                    return result;

                // 6. Aplica os complementos resolvidos na linha PRINCIPAL recém-lançada (a primeira
                // adicionada por AddItemWithPromotion — o item bônus de promoção EmDobro, se houver,
                // não recebe complementos).
                if (resolvedComplements.Count > 0)
                {
                    var primaryItemId = order.Items.ElementAt(itemCountBefore).Id;
                    foreach (var (complementId, extraPrice, _) in resolvedComplements)
                    {
                        var complementResult = order.AddComplement(primaryItemId, complementId, extraPrice, currentTime);
                        if (complementResult.IsFailure)
                            return complementResult;
                    }
                }

                if (stockSnapshot is not null)
                {
                    var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

                    var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
                    if (stockResult.IsFailure) return Result.Failure(stockResult.Error);

                    long? movementEmployeeId = request.EmployeeId != 0 ? request.EmployeeId : null;

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

                    if (movementResult.IsFailure) return Result.Failure(movementResult.Error);

                    _stockRepository.AddMovement(movementResult.Value);
                }

                // 7. Fase 18 (combos) — complementos cujo ComplementItem aponta pra um Product real
                // (LinkedProductId) também baixam o estoque DAQUELE produto, na mesma quantidade do
                // item principal (ex.: 2 combos = baixa 2 unidades do sanduíche vinculado à opção
                // escolhida). Complementos sem LinkedProductId (a maioria — "sem cebola", "bacon
                // extra") não têm produto próprio, então não geram movimentação aqui.
                if (resolvedComplements.Count > 0)
                {
                    var primaryQuantity = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);
                    var complementItemIds = resolvedComplements.Select(c => c.ComplementItemId).Distinct().ToList();
                    var complementItems = await _complementItemRepository.GetByIdsAsync(complementItemIds, cancellationToken);
                    var linkedProductIdsByComplementItemId = complementItems
                        .Where(ci => ci.LinkedProductId.HasValue)
                        .ToDictionary(ci => ci.Id, ci => ci.LinkedProductId!.Value);

                    foreach (var (complementId, _, complementItemId) in resolvedComplements)
                    {
                        if (!linkedProductIdsByComplementItemId.TryGetValue(complementItemId, out var linkedProductId))
                            continue;

                        var linkedStock = await _stockRepository.GetByProductIdAsync(linkedProductId, cancellationToken);
                        if (linkedStock is null)
                            continue; // Produto vinculado não é controlado por estoque — nada a baixar.

                        var linkedStockResult = linkedStock.Deduct(primaryQuantity);
                        if (linkedStockResult.IsFailure)
                            return Result.Failure(linkedStockResult.Error);

                        var linkedMovementEmployeeId = request.EmployeeId is > 0 ? request.EmployeeId : null;
                        var linkedMovementResult = StockMovement.Create(
                            stockItemId: linkedStock.ProductId,
                            stockMovementTypeId: 2, // Tipo: Venda/Saída
                            purchaseItemId: null,
                            orderItemId: order.Items.Last().Id,
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
                    }
                }

                try
                {
                    await _unitOfWork.CommitAsync(cancellationToken);
                }
                catch (ConcurrencyException)
                {
                    return Result.Failure(new Error("Stock.Concurrency",
                        "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
                }

                var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
                if (newItemIds.Any())
                {
                    _ = _printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
                }

                return Result.Success();
            });
    }
}
