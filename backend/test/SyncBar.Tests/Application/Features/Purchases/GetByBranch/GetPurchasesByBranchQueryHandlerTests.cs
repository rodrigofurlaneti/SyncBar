using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Purchases.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Purchases.GetByBranch;

public sealed class GetPurchasesByBranchQueryHandlerTests
{
    private readonly IPurchaseRepository _purchaseRepository = Substitute.For<IPurchaseRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPurchasesByBranchQueryHandler _handler;

    public GetPurchasesByBranchQueryHandlerTests()
    {
        _handler = new GetPurchasesByBranchQueryHandler(_purchaseRepository, _logRepository, _unitOfWork);
    }

    private static Purchase CreatePurchase(
        long branchId = 1,
        long supplierId = 1,
        string? documentNumber = "NF-123",
        string? notes = "Compra mensal")
    {
        var purchasedAt = new DateTime(2026, 9, 1, 10, 0, 0);
        return Purchase.Create(branchId, supplierId, documentNumber, purchasedAt, notes).Value;
    }

    [Fact]
    public async Task Handle_NoPurchasesForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetPurchasesByBranchQuery(BranchId: 1);
        _purchaseRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Purchase>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultiplePurchases_ShouldMapAllFieldsIncludingItems()
    {
        var query = new GetPurchasesByBranchQuery(BranchId: 1);

        var purchaseWithItems = CreatePurchase(supplierId: 10, documentNumber: "NF-001", notes: "Bebidas");
        purchaseWithItems.AddItem(productId: 100, quantity: 5m, unitCost: 2.5m);
        purchaseWithItems.AddItem(productId: 200, quantity: 3m, unitCost: 4m);

        var purchaseWithoutOptionalFields = Purchase.Create(
            branchId: 1, supplierId: 20, documentNumber: null, purchasedAt: new DateTime(2026, 9, 2), notes: null).Value;

        _purchaseRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([purchaseWithItems, purchaseWithoutOptionalFields]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var responseWithItems = result.Value.First(r => r.SupplierId == 10);
        responseWithItems.DocumentNumber.Should().Be("NF-001");
        responseWithItems.Notes.Should().Be("Bebidas");
        responseWithItems.PurchasedAt.Should().Be(purchaseWithItems.PurchasedAt);
        responseWithItems.TotalAmount.Should().Be(purchaseWithItems.TotalAmount);
        responseWithItems.Items.Should().HaveCount(2);

        var item1 = responseWithItems.Items.First(i => i.ProductId == 100);
        item1.Quantity.Should().Be(5m);
        item1.UnitCost.Should().Be(2.5m);
        item1.TotalCost.Should().Be(12.5m);

        var item2 = responseWithItems.Items.First(i => i.ProductId == 200);
        item2.Quantity.Should().Be(3m);
        item2.UnitCost.Should().Be(4m);
        item2.TotalCost.Should().Be(12m);

        var responseWithoutOptionalFields = result.Value.First(r => r.SupplierId == 20);
        responseWithoutOptionalFields.DocumentNumber.Should().BeNull();
        responseWithoutOptionalFields.Notes.Should().BeNull();
        responseWithoutOptionalFields.Items.Should().BeEmpty();
    }
}
