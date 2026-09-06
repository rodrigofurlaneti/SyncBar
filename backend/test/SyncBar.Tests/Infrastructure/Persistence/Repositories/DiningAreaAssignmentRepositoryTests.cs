using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class DiningAreaAssignmentRepositoryTests : RepositoryTestBase
    {
        private readonly DiningAreaAssignmentRepository _repository;

        public DiningAreaAssignmentRepositoryTests()
        {
            _repository = new DiningAreaAssignmentRepository(Context);
        }

        private static DiningAreaAssignment CreateAssignment(long diningAreaId = 1, long employeeId = 1) =>
            DiningAreaAssignment.Create(diningAreaId, employeeId, DateTime.Now).Value;

        private async Task<DiningAreaAssignment> SeedAsync(DiningAreaAssignment assignment)
        {
            await Context.AddAsync(assignment);
            await Context.SaveChangesAsync();
            Context.Entry(assignment).State = EntityState.Detached;
            return assignment;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsTrackedAssignment()
        {
            var assignment = await SeedAsync(CreateAssignment());

            var result = await _repository.GetByIdAsync(assignment.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(assignment.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveByEmployeeIdAsync_ActiveOpenAssignment_ReturnsAssignment()
        {
            var assignment = await SeedAsync(CreateAssignment(employeeId: 5));

            var result = await _repository.GetActiveByEmployeeIdAsync(5);

            result.Should().ContainSingle(x => x.Id == assignment.Id);
        }

        [Fact]
        public async Task GetActiveByEmployeeIdAsync_NoAssignmentsForEmployee_ReturnsEmptyList()
        {
            var result = await _repository.GetActiveByEmployeeIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveByDiningAreaIdAsync_ActiveOpenAssignment_ReturnsAssignment()
        {
            var assignment = await SeedAsync(CreateAssignment(diningAreaId: 10));

            var result = await _repository.GetActiveByDiningAreaIdAsync(10);

            result.Should().ContainSingle(x => x.Id == assignment.Id);
        }

        [Fact]
        public async Task GetActiveByDiningAreaIdAsync_NoAssignmentsForArea_ReturnsEmptyList()
        {
            var result = await _repository.GetActiveByDiningAreaIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidAssignment_PersistsToDatabase()
        {
            var assignment = CreateAssignment(diningAreaId: 20, employeeId: 20);

            await _repository.AddAsync(assignment);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningAreaAssignment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DiningAreaId == 20 && x.EmployeeId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingAssignment_PersistsChanges()
        {
            var assignment = await SeedAsync(CreateAssignment());
            var tracked = await Context.Set<DiningAreaAssignment>().FirstAsync(x => x.Id == assignment.Id);
            tracked.Deactivate();

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningAreaAssignment>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == assignment.Id);

            persisted.IsActive.Should().BeFalse();
        }
    }
}
