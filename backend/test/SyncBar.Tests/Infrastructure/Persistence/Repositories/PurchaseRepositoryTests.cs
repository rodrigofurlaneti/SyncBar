using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class PurchaseRepositoryTests : RepositoryTestBase
    {
        private readonly PurchaseRepository _repository;

        public PurchaseRepositoryTests()
        {
            _repository = new PurchaseRepository(Context);
        }

        private static Purchase CreatePurchase(long branchId = 1, DateTime? purchasedAt = null)
        {
            var purchase = Purchase.Create(branchId, supplierId: 1, documentNumber: "NF-001", purchasedAt ?? DateTime.Now, notes: null).Value;
            purchase.AddItem(productId: 1, quantity: 10m, unitCost: 5m);
            return purchase;
        }

        private async Task<Purchase> SeedAsync(Purchase purchase)
        {
            await Context.AddAsync(purchase);
            await Context.SaveChangesAsync();
            Context.Entry(purchase).State = EntityState.Detached;
            return purchase;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedPurchaseWithItems()
        {
            var purchase = await SeedAsync(CreatePurchase());

            var result = await _repository.GetByIdAsync(purchase.Id);

            result.Should().NotBeNull();
            result!.Items.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActivePurchases_ReturnsOrderedByPurchasedAtDescending()
        {
            var older = await SeedAsync(CreatePurchase(branchId: 5, purchasedAt: DateTime.Now.AddDays(-2)));
            var newer = await SeedAsync(CreatePurchase(branchId: 5, purchasedAt: DateTime.Now.AddDays(-1)));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(newer.Id);
            result.ElementAt(1).Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetByBranchAsync_InactivePurchase_IsExcluded()
        {
            var purchase = await SeedAsync(CreatePurchase(branchId: 5));
            var tracked = await Context.Set<Purchase>().FirstAsync(x => x.Id == purchase.Id);
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoPurchasesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidPurchase_PersistsToDatabase()
        {
            var purchase = CreatePurchase(branchId: 7);

            await _repository.AddAsync(purchase);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Purchase>()
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.Items.Should().ContainSingle();
        }
    }
}
