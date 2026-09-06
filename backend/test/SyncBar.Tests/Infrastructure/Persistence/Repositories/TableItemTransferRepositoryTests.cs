using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class TableItemTransferRepositoryTests : RepositoryTestBase
    {
        private readonly TableItemTransferRepository _repository;

        public TableItemTransferRepositoryTests()
        {
            _repository = new TableItemTransferRepository(Context);
        }

        [Fact]
        public async Task AddAsync_ValidTransfer_PersistsToDatabase()
        {
            var transfer = TableItemTransfer.Create(
                customerOrderId: 1, customerOrderItemId: 1, sourceDiningTableId: 1, targetDiningTableId: 2, employeeId: 1).Value;

            await _repository.AddAsync(transfer);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<TableItemTransfer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourceDiningTableId == 1 && x.TargetDiningTableId == 2);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
