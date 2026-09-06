using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class PromotionRepositoryTests : RepositoryTestBase
    {
        private readonly PromotionRepository _repository;

        public PromotionRepositoryTests()
        {
            _repository = new PromotionRepository(Context);
        }

        private static Promotion CreatePromotion(long branchId = 1, long productId = 1) =>
            Promotion.Create(branchId, productId, "Terça em dobro", dayOfWeek: 2, startMinuteOfDay: 0, endMinuteOfDay: 1439).Value;

        private async Task<Promotion> SeedAsync(Promotion promotion)
        {
            await Context.AddAsync(promotion);
            await Context.SaveChangesAsync();
            Context.Entry(promotion).State = EntityState.Detached;
            return promotion;
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedPromotion()
        {
            var promotion = await SeedAsync(CreatePromotion());

            var result = await _repository.GetByIdForUpdateAsync(promotion.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActivePromotions_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreatePromotion(branchId: 5, productId: 1));
            var second = await SeedAsync(CreatePromotion(branchId: 5, productId: 2));
            await SeedAsync(CreatePromotion(branchId: 6, productId: 3));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_DifferentTenantBranch_ReturnsEmptyList()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            await SeedAsync(CreatePromotion(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByBranchAsync(branch.Id);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoPromotionsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidPromotion_PersistsToDatabase()
        {
            var promotion = CreatePromotion(branchId: 7, productId: 77);

            await _repository.AddAsync(promotion);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Promotion>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7 && x.ProductId == 77);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
