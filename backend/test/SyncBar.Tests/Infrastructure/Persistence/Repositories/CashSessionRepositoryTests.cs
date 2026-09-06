using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CashSessionRepositoryTests : RepositoryTestBase
    {
        private readonly CashSessionRepository _repository;

        public CashSessionRepositoryTests()
        {
            _repository = new CashSessionRepository(Context);
        }

        private static CashSession CreateSession(long cashRegisterId = 1) =>
            CashSession.Open(cashRegisterId, openedByEmployeeId: 1, openingAmount: 100m).Value;

        private async Task<CashSession> SeedAsync(CashSession session)
        {
            await Context.AddAsync(session);
            await Context.SaveChangesAsync();
            Context.Entry(session).State = EntityState.Detached;
            return session;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedSession()
        {
            var session = await SeedAsync(CreateSession());

            var result = await _repository.GetByIdAsync(session.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedSession()
        {
            var session = await SeedAsync(CreateSession());

            var result = await _repository.GetByIdForUpdateAsync(session.Id);

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
        public async Task GetOpenByCashRegisterAsync_OpenSessionExists_ReturnsSession()
        {
            var session = await SeedAsync(CreateSession(cashRegisterId: 5));

            var result = await _repository.GetOpenByCashRegisterAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(session.Id);
        }

        [Fact]
        public async Task GetOpenByCashRegisterAsync_NoOpenSession_ReturnsNull()
        {
            var result = await _repository.GetOpenByCashRegisterAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_SessionForRegisterInBranch_ReturnsSession()
        {
            var register = CashRegister.Create(5, "Caixa 1").Value;
            await Context.AddAsync(register);
            await Context.SaveChangesAsync();
            var session = await SeedAsync(CreateSession(cashRegisterId: register.Id));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().ContainSingle(x => x.Id == session.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_RegisterFromDifferentBranch_ReturnsEmptyList()
        {
            var register = CashRegister.Create(5, "Caixa 1").Value;
            await Context.AddAsync(register);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateSession(cashRegisterId: register.Id));

            var result = await _repository.GetByBranchAndPeriodAsync(
                6, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OutsidePeriod_ReturnsEmptyList()
        {
            var register = CashRegister.Create(5, "Caixa 1").Value;
            await Context.AddAsync(register);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateSession(cashRegisterId: register.Id));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, DateTime.Now.AddDays(10), DateTime.Now.AddDays(20));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidSession_PersistsToDatabase()
        {
            var session = CreateSession(cashRegisterId: 7);

            await _repository.AddAsync(session);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CashSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CashRegisterId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
