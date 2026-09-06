using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AsaasIntegrationSavedCardRepositoryTests : RepositoryTestBase
    {
        private readonly AsaasIntegrationSavedCardRepository _repository;

        public AsaasIntegrationSavedCardRepositoryTests()
        {
            _repository = new AsaasIntegrationSavedCardRepository(Context);
        }

        private static AsaasIntegrationSavedCard CreateSavedCard(
            long customerId = 1, long companyId = 1, string token = "token-1") =>
            AsaasIntegrationSavedCard.Create(customerId, companyId, token, "VISA", "1234").Value;

        private async Task<AsaasIntegrationSavedCard> SeedAsync(AsaasIntegrationSavedCard card)
        {
            await Context.AddAsync(card);
            await Context.SaveChangesAsync();
            Context.Entry(card).State = EntityState.Detached;
            return card;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingActiveCard_ReturnsUntrackedCard()
        {
            var card = await SeedAsync(CreateSavedCard());

            var result = await _repository.GetByIdAsync(card.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_DeactivatedCard_ReturnsNull()
        {
            var card = await SeedAsync(CreateSavedCard());
            var tracked = await _repository.GetByIdForUpdateAsync(card.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(card.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByTokenAsync_ExistingToken_ReturnsCard()
        {
            var card = await SeedAsync(CreateSavedCard(token: "token-abc"));

            var result = await _repository.GetByTokenAsync("token-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(card.Id);
        }

        [Fact]
        public async Task GetByTokenAsync_NonExistingToken_ReturnsNull()
        {
            var result = await _repository.GetByTokenAsync("token-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCustomerIdAsync_MultipleCards_ReturnsOrderedByCreatedAtDescending()
        {
            var older = await SeedAsync(CreateSavedCard(customerId: 5, token: "token-old"));
            await Task.Delay(10);
            var newer = await SeedAsync(CreateSavedCard(customerId: 5, token: "token-new"));

            var result = await _repository.GetByCustomerIdAsync(5);

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(newer.Id);
            result[1].Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetByCustomerIdAsync_NoCardsForCustomer_ReturnsEmptyList()
        {
            var result = await _repository.GetByCustomerIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCustomerIdAndCompanyIdForUpdateAsync_MatchingBoth_ReturnsTrackedCards()
        {
            var card = await SeedAsync(CreateSavedCard(customerId: 5, companyId: 7, token: "token-x"));

            var result = await _repository.GetByCustomerIdAndCompanyIdForUpdateAsync(5, 7);

            result.Should().ContainSingle(x => x.Id == card.Id);
            Context.Entry(result[0]).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByCustomerIdAndCompanyIdForUpdateAsync_DifferentCompany_ReturnsEmptyList()
        {
            await SeedAsync(CreateSavedCard(customerId: 5, companyId: 7));

            var result = await _repository.GetByCustomerIdAndCompanyIdForUpdateAsync(5, 8);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByTokenAsync_ExistingActiveToken_ReturnsTrue()
        {
            await SeedAsync(CreateSavedCard(token: "token-exists"));

            var result = await _repository.ExistsByTokenAsync("token-exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByTokenAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByTokenAsync("token-missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedCard()
        {
            var card = await SeedAsync(CreateSavedCard());

            var result = await _repository.GetByIdForUpdateAsync(card.Id);

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
        public async Task AddAsync_ValidCard_PersistsToDatabase()
        {
            var card = CreateSavedCard(token: "token-added");

            await _repository.AddAsync(card);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CreditCardToken == "token-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingCard_PersistsChanges()
        {
            var card = await SeedAsync(CreateSavedCard());
            var tracked = await _repository.GetByIdForUpdateAsync(card.Id);
            tracked!.UpdateDetails(holderName: "New Holder");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == card.Id);

            persisted.HolderName.Should().Be("New Holder");
        }

        [Fact]
        public async Task Delete_ExistingCard_RemovesFromDatabase()
        {
            var card = await SeedAsync(CreateSavedCard());
            var tracked = await _repository.GetByIdForUpdateAsync(card.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == card.Id);

            persisted.Should().BeNull();
        }
    }
}
