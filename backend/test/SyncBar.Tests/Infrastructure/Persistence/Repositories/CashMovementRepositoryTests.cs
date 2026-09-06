using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CashMovementRepositoryTests : RepositoryTestBase
    {
        private readonly CashMovementRepository _repository;

        public CashMovementRepositoryTests()
        {
            _repository = new CashMovementRepository(Context);
        }

        private static CashMovement CreateMovement(long cashSessionId = 1) =>
            CashMovement.Create(cashSessionId, cashMovementTypeId: 1, saleId: null, employeeId: 1, amount: 50m, description: "Sangria").Value;

        private async Task<CashMovement> SeedAsync(CashMovement movement)
        {
            await Context.AddAsync(movement);
            await Context.SaveChangesAsync();
            Context.Entry(movement).State = EntityState.Detached;
            return movement;
        }

        [Fact]
        public async Task GetBySessionAsync_MultipleActiveMovements_ReturnsAllForSession()
        {
            var first = await SeedAsync(CreateMovement(cashSessionId: 5));
            var second = await SeedAsync(CreateMovement(cashSessionId: 5));
            await SeedAsync(CreateMovement(cashSessionId: 6));

            var result = await _repository.GetBySessionAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetBySessionAsync_InactiveMovement_IsExcluded()
        {
            var movement = await SeedAsync(CreateMovement(cashSessionId: 5));
            movement.Deactivate();
            Context.Update(movement);
            await Context.SaveChangesAsync();

            var result = await _repository.GetBySessionAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBySessionAsync_NoMovementsForSession_ReturnsEmptyList()
        {
            var result = await _repository.GetBySessionAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMovement_PersistsToDatabase()
        {
            var movement = CreateMovement(cashSessionId: 7);

            await _repository.AddAsync(movement);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CashMovement>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CashSessionId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
