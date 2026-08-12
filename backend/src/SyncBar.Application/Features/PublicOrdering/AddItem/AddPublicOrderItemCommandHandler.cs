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
    private readonly IPrintingService _printingService;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AddPublicOrderItemCommandHandler(
        IDiningTableRepository diningTableRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        ICustomerOrderRepository orderRepository,
        IPrintingService printingService,
        TimeProvider timeProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _diningTableRepository = diningTableRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _printingService = printingService;
        _timeProvider = timeProvider;
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

                var currentTime = _timeProvider.GetLocalNow().DateTime;

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