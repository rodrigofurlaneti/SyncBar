using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Purchases.Register;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Purchases.Register;

public sealed class RegisterPurchaseCommandHandlerTests
{
    private readonly IPurchaseRepository _purchaseRepository = Substitute.For<IPurchaseRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterPurchaseCommandHandler _handler;

    public RegisterPurchaseCommandHandlerTests()
    {
        _handler = new RegisterPurchaseCommandHandler(
            _purchaseRepository, _stockItemRepository, _stockMovementRepository, _logRepository, _unitOfWork);
    }

    private static RegisterPurchaseCommand CreateValidCommand(IReadOnlyCollection<PurchaseItemInput>? items = null)
        => new(
            BranchId: 1,
            SupplierId: 10,
            EmployeeId: 99,
            DocumentNumber: "NF-777",
            PurchasedAt: new DateTime(2026, 9, 1, 9, 0, 0),
            Notes: "Compra de estoque",
            Items: items ?? [new PurchaseItemInput(ProductId: 100, Quantity: 5m, UnitCost: 2.5m)]);

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    [Fact]
    public async Task Handle_ItemWithInvalidQuantity_ShouldReturnFailureWithoutPersistingAnything()
    {
        // AddItem falha antes de qualquer chamada a repositório — nem a compra chega a ser salva.
        var command = CreateValidCommand(items: [new PurchaseItemInput(ProductId: 100, Quantity: 0m, UnitCost: 2.5m)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Purchase.InvalidQuantity");
        await _purchaseRepository.DidNotReceive().AddAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
        await _stockItemRepository.DidNotReceive().AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        // Nenhuma persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemWithNegativeUnitCost_ShouldReturnFailureWithoutPersistingAnything()
    {
        var command = CreateValidCommand(items: [new PurchaseItemInput(ProductId: 100, Quantity: 5m, UnitCost: -1m)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Purchase.InvalidUnitCost");
        await _purchaseRepository.DidNotReceive().AddAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingStockItem_ShouldIncreaseBalanceAndRegisterMovementWithoutCreatingStockItem()
    {
        var command = CreateValidCommand();

        var existingStockItem = StockItem.Create(branchId: 1, productId: 100, minimumQuantity: 0, maximumQuantity: null).Value;
        SetId(existingStockItem, 555);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 100, Arg.Any<CancellationToken>())
            .Returns(existingStockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _purchaseRepository.Received(1).AddAsync(
            Arg.Is<Purchase>(p =>
                p.BranchId == command.BranchId &&
                p.SupplierId == command.SupplierId &&
                p.DocumentNumber == command.DocumentNumber &&
                p.Notes == command.Notes &&
                p.Items.Count == 1 &&
                p.Items.First().ProductId == 100 &&
                p.Items.First().Quantity == 5m &&
                p.Items.First().UnitCost == 2.5m &&
                p.Items.First().TotalCost == 12.5m),
            Arg.Any<CancellationToken>());

        // Item já existia: não deve ser criado de novo.
        await _stockItemRepository.DidNotReceive().AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        existingStockItem.CurrentQuantity.Should().Be(5m);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m =>
                m.StockItemId == 555 &&
                m.StockMovementTypeId == StockMovementTypeIds.EntradaCompra &&
                m.EmployeeId == command.EmployeeId &&
                m.Quantity == 5m &&
                m.UnitCost == 2.5m &&
                m.TotalCost == 12.5m &&
                m.DocumentNumber == command.DocumentNumber &&
                m.MovedAt == command.PurchasedAt &&
                m.Notes == command.Notes),
            Arg.Any<CancellationToken>());

        // Commit após persistir a compra (linha explícita) + commit explícito após registrar
        // as entradas de estoque + commit do finally da base = 3 (item já existia, sem commit
        // extra de criação de StockItem).
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleItemsSomeWithoutExistingStockItem_ShouldCreateMissingStockItemWithExtraCommit()
    {
        var command = CreateValidCommand(items:
        [
            new PurchaseItemInput(ProductId: 100, Quantity: 5m, UnitCost: 2.5m),
            new PurchaseItemInput(ProductId: 200, Quantity: 3m, UnitCost: 4m)
        ]);

        var existingStockItem = StockItem.Create(branchId: 1, productId: 100, minimumQuantity: 0, maximumQuantity: null).Value;
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 100, Arg.Any<CancellationToken>())
            .Returns(existingStockItem);
        // Produto 200 ainda não tem StockItem cadastrado nesta filial.
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 200, Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _stockItemRepository.Received(1).AddAsync(
            Arg.Is<StockItem>(s => s.BranchId == 1 && s.ProductId == 200 && s.CurrentQuantity == 3m),
            Arg.Any<CancellationToken>());

        existingStockItem.CurrentQuantity.Should().Be(5m);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Quantity == 5m && m.UnitCost == 2.5m && m.TotalCost == 12.5m),
            Arg.Any<CancellationToken>());
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Quantity == 3m && m.UnitCost == 4m && m.TotalCost == 12m),
            Arg.Any<CancellationToken>());

        // Commit após persistir a compra + commit da criação do StockItem novo (produto 200,
        // dentro de GetOrCreateStockItemAsync) + commit explícito após registrar as entradas de
        // estoque + commit do finally da base = 4.
        await _unitOfWork.Received(4).CommitAsync(Arg.Any<CancellationToken>());
    }
}
