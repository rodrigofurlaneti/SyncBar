using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Storefront.AddOrder
{
    internal sealed class AddWebStorefrontOrderCommandHandler : BaseCommandHandler<AddWebStorefrontOrderCommand, long>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICustomerOrderRepository _orderRepository;
        private readonly ICustomerAddressRepository _customerAddressRepository; // <-- Adicionado
        private readonly IProductComplementGroupRepository _productComplementGroupRepository;
        private readonly IComplementGroupRepository _complementGroupRepository;
        private readonly IPrintingService _printingService;
        private readonly TimeProvider _timeProviderCustom;
        private readonly IUnitOfWork _unitOfWork;

        public AddWebStorefrontOrderCommandHandler(
            IBranchRepository branchRepository,
            IProductRepository productRepository,
            ICustomerOrderRepository orderRepository,
            ICustomerAddressRepository customerAddressRepository,
            IProductComplementGroupRepository productComplementGroupRepository,
            IComplementGroupRepository complementGroupRepository,
            IPrintingService printingService,
            TimeProvider timeProviderCustom,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _branchRepository = branchRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _customerAddressRepository = customerAddressRepository;
            _productComplementGroupRepository = productComplementGroupRepository;
            _complementGroupRepository = complementGroupRepository;
            _printingService = printingService;
            _timeProviderCustom = timeProviderCustom;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<long>> Handle(AddWebStorefrontOrderCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(AddWebStorefrontOrderCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.Items is not { Count: > 0 })
                        return Result.Failure<long>(new Error("Cart.Empty", "O carrinho está vazio."));
                    var branchResult = await ValidateBranchAsync(request.BranchId, cancellationToken);
                    if (branchResult.IsFailure)
                        return Result.Failure<long>(branchResult.Error);
                    var branch = branchResult.Value;
                    userIdBox.Value = branch.SelfServiceEmployeeId.GetValueOrDefault();
                    var currentTime = _timeProviderCustom.GetLocalNow().DateTime;
                    string? deliveryAddressFormatted = null;
                    CustomerAddress? customerAddress = null;
                    if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                    {
                        var addresses = await _customerAddressRepository.GetByCustomerIdAsync(request.CustomerId.Value, cancellationToken);
                        customerAddress = addresses.FirstOrDefault(x => x.IsActive);

                        if (customerAddress != null)
                        {
                            deliveryAddressFormatted = $"{customerAddress.Street}, Nº {customerAddress.Number}";
                            if (!string.IsNullOrWhiteSpace(customerAddress.Supplement))
                            {
                                deliveryAddressFormatted += $" - {customerAddress.Supplement}";
                            }
                        }
                    }

                    var createdOrderResult = CustomerOrder.Create(
                        branchId: branch.Id,
                        diningTableId: null,
                        comandaId: null,
                        employeeId: branch.SelfServiceEmployeeId.GetValueOrDefault(),
                        guestCount: null,
                        notes: request.GeneralNotes,
                        Now: currentTime,
                        creditLimitAmount: null,
                        orderTypeId: OrderTypeIds.WebSite,
                        customerName: request.CustomerName,
                        customerPhone: request.CustomerPhone,
                        deliveryAddress: deliveryAddressFormatted,
                        customerId: request.CustomerId); 

                    if (createdOrderResult.IsFailure)
                        return Result.Failure<long>(createdOrderResult.Error);

                    var order = createdOrderResult.Value;

                    foreach (var itemReq in request.Items)
                    {
                        var productResult = await ValidateProductAsync(itemReq.ProductId, branch.CompanyId, cancellationToken);
                        if (productResult.IsFailure)
                            return Result.Failure<long>(productResult.Error);

                        var product = productResult.Value;
                        var complementsResult = await ResolveComplementsAsync(product, itemReq, cancellationToken);
                        if (complementsResult.IsFailure)
                            return Result.Failure<long>(complementsResult.Error);

                        var resolvedComplements = complementsResult.Value;
                        var itemNotes = string.IsNullOrWhiteSpace(itemReq.Notes) ? request.GeneralNotes : $"{itemReq.Notes} ({request.GeneralNotes})";
                        var added = order.AddItem(product.Id, product.SalePrice, itemReq.Quantity, itemNotes, null, currentTime);
                        if (added.IsFailure)
                            return Result.Failure<long>(added.Error);

                        var addedItem = order.Items.Last();
                        foreach (var (complementId, extraPrice) in resolvedComplements)
                        {
                            var complementResult = order.AddComplement(addedItem.Id, complementId, extraPrice, currentTime);
                            if (complementResult.IsFailure)
                                return Result.Failure<long>(complementResult.Error);
                        }
                    }

                    // 3. Salva o pedido e gera o order.Id
                    await _orderRepository.AddAsync(order, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    // 4. Atualiza o LastOrderId no endereço do cliente com o ID do pedido recém-criado
                    if (customerAddress != null)
                    {
                        customerAddress.RegisterOrderUsage(order.Id);
                        await _customerAddressRepository.UpdateAsync(customerAddress, cancellationToken);
                        await _unitOfWork.CommitAsync(cancellationToken);
                    }

                    try
                    {
                        var newItemIds = order.Items.Select(i => i.Id).ToList();
                        await _printingService.PrintOrderItemsAsync(order.Id, newItemIds, cancellationToken);
                    }
                    catch
                    {
                    }

                    return Result.Success(order.Id);
                });
        }

        private async Task<Result<Branch>> ValidateBranchAsync(long branchId, CancellationToken cancellationToken)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
            if (branch is null || !branch.IsActive)
                return Result.Failure<Branch>(new Error("Branch.NotFound", "Branch not found."));
            if (!branch.SelfServiceEmployeeId.HasValue)
                return Result.Failure<Branch>(new Error("Branch.SelfServiceDisabled", "Self-service ordering is not enabled for this branch."));
            return Result.Success(branch);
        }

        private async Task<Result<Product>> ValidateProductAsync(long productId, long companyId, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null || !product.IsActive || product.CompanyId != companyId)
                return Result.Failure<Product>(new Error("Product.NotFound", "Product not found."));
            return Result.Success(product);
        }

        private async Task<Result<List<(long ComplementId, decimal ExtraPrice)>>> ResolveComplementsAsync(
            Product product, WebStorefrontItemDto itemReq, CancellationToken cancellationToken)
        {
            var resolvedComplements = new List<(long ComplementId, decimal ExtraPrice)>();
            if (itemReq.Complements is not { Count: > 0 })
                return Result.Success(resolvedComplements);

            var links = await _productComplementGroupRepository.GetByProductAsync(product.Id, cancellationToken);
            var allowedGroupIds = links.Select(l => l.ComplementGroupId).ToHashSet();

            foreach (var selection in itemReq.Complements)
            {
                if (!allowedGroupIds.Contains(selection.ComplementGroupId))
                    return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("OrderItem.ComplementGroupNotAvailable", "Complement group not available."));

                var group = await _complementGroupRepository.GetByIdAsync(selection.ComplementGroupId, cancellationToken);
                if (group is null || !group.IsActive)
                    return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("ComplementGroup.NotFound", "Complement group not found."));

                var complement = group.Complements.FirstOrDefault(c => c.Id == selection.ComplementId && c.IsActive);
                if (complement is null)
                    return Result.Failure<List<(long ComplementId, decimal ExtraPrice)>>(new Error("ComplementGroup.ComplementNotFound", "Complement not found."));

                resolvedComplements.Add((complement.Id, complement.ExtraPrice));
            }

            return Result.Success(resolvedComplements);
        }
    }
}