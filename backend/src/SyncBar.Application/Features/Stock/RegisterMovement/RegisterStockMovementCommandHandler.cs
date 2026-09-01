using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Security; // ajuste o namespace conforme onde ICurrentUserService estiver
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.RegisterMovement;

internal sealed class RegisterStockMovementCommandHandler : BaseCommandHandler<RegisterStockMovementCommand, long>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly HashSet<long> InflowTypes =
    [
        StockMovementTypeIds.EntradaCompra,
        StockMovementTypeIds.AjusteEntrada,
        StockMovementTypeIds.TransferenciaEntrada
    ];

    public RegisterStockMovementCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUser,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _productRepository = productRepository;
        _employeeRepository = employeeRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(RegisterStockMovementCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RegisterStockMovementCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var employeeResult = await ValidateEmployeeAsync(cancellationToken);
                if (employeeResult.IsFailure)
                    return Result.Failure<long>(employeeResult.Error);

                var employee = employeeResult.Value;
                userIdBox.Value = employee.Id;

                var productResult = await ValidateProductAsync(request.ProductId, cancellationToken);
                if (productResult.IsFailure)
                    return Result.Failure<long>(productResult.Error);

                var stockItemResult = await GetOrCreateStockItemAsync(request, cancellationToken);
                if (stockItemResult.IsFailure)
                    return Result.Failure<long>(stockItemResult.Error);

                var stockItem = stockItemResult.Value;

                var balanceResult = ApplyMovementToStock(stockItem, request);
                if (balanceResult.IsFailure)
                    return Result.Failure<long>(balanceResult.Error);

                var movementResult = CreateStockMovement(stockItem.Id, employee.Id, request);
                if (movementResult.IsFailure)
                    return Result.Failure<long>(movementResult.Error);

                await _stockMovementRepository.AddAsync(movementResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(movementResult.Value.Id);
            });
    }

    private async Task<Result<Employee>> ValidateEmployeeAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.EmployeeId is not { } employeeId)
            return Result.Failure<Employee>(new Error(
                "Employee.NotFound",
                "O usuário logado não possui um funcionário vinculado."));

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
            return Result.Failure<Employee>(new Error(
                "Employee.NotFound",
                "Funcionário vinculado ao usuário logado não está ativo."));

        return Result.Success(employee);
    }

    private async Task<Result<Product>> ValidateProductAsync(long productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<Product>(new Error("Product.NotFound", "Product not found."));

        return Result.Success(product);
    }

    private async Task<Result<StockItem>> GetOrCreateStockItemAsync(
        RegisterStockMovementCommand request, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(
            request.BranchId, request.ProductId, cancellationToken);

        if (stockItem is not null)
            return Result.Success(stockItem);

        var created = StockItem.Create(request.BranchId, request.ProductId, 0, null);
        if (created.IsFailure)
            return Result.Failure<StockItem>(created.Error);

        stockItem = created.Value;
        await _stockItemRepository.AddAsync(stockItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(stockItem);
    }

    private static Result ApplyMovementToStock(StockItem stockItem, RegisterStockMovementCommand request)
    {
        var isInflow = InflowTypes.Contains(request.StockMovementTypeId);
        return isInflow ? stockItem.Increase(request.Quantity) : stockItem.Decrease(request.Quantity);
    }

    private static Result<StockMovement> CreateStockMovement(
        long stockItemId, long employeeId, RegisterStockMovementCommand request)
    {
        return StockMovement.Create(
            stockItemId,
            request.StockMovementTypeId,
            null,
            null,
            employeeId,
            request.Quantity, request.UnitCost,
            request.UnitCost is null ? null : Math.Round(request.UnitCost.Value * request.Quantity, 2),
            request.DocumentNumber, DateTime.Now, request.Notes);
    }
}