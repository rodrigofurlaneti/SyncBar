using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class TableReservationRepositoryTests : RepositoryTestBase
    {
        private readonly TableReservationRepository _repository;

        public TableReservationRepositoryTests()
        {
            _repository = new TableReservationRepository(Context);
        }

        private static TableReservation CreateReservation(long branchId = 1, DateTime? reservedFor = null) =>
            TableReservation.Create(
                branchId, diningTableId: 1, customerName: "Cliente Teste", customerPhone: null,
                partySize: 4, reservedFor: reservedFor ?? DateTime.Now.AddDays(1), notes: null).Value;

        private async Task<TableReservation> SeedAsync(TableReservation reservation)
        {
            await Context.AddAsync(reservation);
            await Context.SaveChangesAsync();
            Context.Entry(reservation).State = EntityState.Detached;
            return reservation;
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedReservation()
        {
            var reservation = await SeedAsync(CreateReservation());

            var result = await _repository.GetByIdForUpdateAsync(reservation.Id);

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
        public async Task GetByBranchAndDateAsync_ReservationWithinRange_ReturnsOrderedByReservedFor()
        {
            var later = await SeedAsync(CreateReservation(branchId: 5, reservedFor: DateTime.Now.AddDays(3)));
            var earlier = await SeedAsync(CreateReservation(branchId: 5, reservedFor: DateTime.Now.AddDays(1)));

            var result = await _repository.GetByBranchAndDateAsync(
                5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(earlier.Id);
            result.ElementAt(1).Id.Should().Be(later.Id);
        }

        [Fact]
        public async Task GetByBranchAndDateAsync_InactiveReservation_IsExcluded()
        {
            var reservation = await SeedAsync(CreateReservation(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(reservation.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAndDateAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndDateAsync_OutsideRange_ReturnsEmptyList()
        {
            await SeedAsync(CreateReservation(branchId: 5, reservedFor: DateTime.Now.AddDays(20)));

            var result = await _repository.GetByBranchAndDateAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndDateAsync_NoReservationsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAndDateAsync(999, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidReservation_PersistsToDatabase()
        {
            var reservation = CreateReservation(branchId: 7);

            await _repository.AddAsync(reservation);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<TableReservation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
