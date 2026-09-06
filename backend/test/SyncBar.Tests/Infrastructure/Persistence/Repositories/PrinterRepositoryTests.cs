using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class PrinterRepositoryTests : RepositoryTestBase
    {
        private readonly PrinterRepository _repository;

        public PrinterRepositoryTests()
        {
            _repository = new PrinterRepository(Context);
        }

        private static Printer CreatePrinter(long branchId = 1, string name = "Impressora Cozinha") =>
            Printer.Create(branchId, name, connectionType: Printer.ConnectionWindows, printerName: "Driver-1", ipAddress: null, port: null, printsOrders: true, printsBills: false).Value;

        private async Task<Printer> SeedAsync(Printer printer)
        {
            await Context.AddAsync(printer);
            await Context.SaveChangesAsync();
            Context.Entry(printer).State = EntityState.Detached;
            return printer;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedPrinter()
        {
            var printer = await SeedAsync(CreatePrinter());

            var result = await _repository.GetByIdAsync(printer.Id);

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
        public async Task GetByIdAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            var printer = await SeedAsync(CreatePrinter(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(printer.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedPrinter()
        {
            var printer = await SeedAsync(CreatePrinter());

            var result = await _repository.GetByIdForUpdateAsync(printer.Id);

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
        public async Task GetByBranchAsync_MultipleActivePrinters_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreatePrinter(branchId: 5, name: "P1"));
            var second = await SeedAsync(CreatePrinter(branchId: 5, name: "P2"));
            await SeedAsync(CreatePrinter(branchId: 6, name: "P3"));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactivePrinter_IsExcluded()
        {
            var printer = await SeedAsync(CreatePrinter(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(printer.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoPrintersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidPrinter_PersistsToDatabase()
        {
            var printer = CreatePrinter(branchId: 7, name: "Nova Impressora");

            await _repository.AddAsync(printer);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Printer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Nova Impressora");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
