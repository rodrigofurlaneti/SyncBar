using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class WaiterMessageConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(WaiterMessage))!;

        [Fact]
        public void Configure_ShouldMapToWaiterMessageTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("waitermessage");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(WaiterMessage.Id));
            EntityType.FindProperty(nameof(WaiterMessage.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_BranchIdAndSenderEmployeeId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(WaiterMessage.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(WaiterMessage.SenderEmployeeId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_Message_ShouldBeRequiredVarchar500()
        {
            var message = EntityType.FindProperty(nameof(WaiterMessage.Message))!;

            message.IsNullable.Should().BeFalse();
            message.GetColumnType().Should().Be("varchar(500)");
        }

        [Fact]
        public void Configure_IsRead_ShouldBeRequiredTinyint()
        {
            var isRead = EntityType.FindProperty(nameof(WaiterMessage.IsRead))!;

            isRead.IsNullable.Should().BeFalse();
            isRead.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(WaiterMessage.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(WaiterMessage.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(WaiterMessage.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_WaiterMessage_BranchId" &&
                i.Properties.Single().Name == nameof(WaiterMessage.BranchId));
        }

        // A configuração real não declara nenhum HasOne/HasForeignKey — nem para BranchId, nem
        // para SenderEmployeeId/RecipientEmployeeId/DiningAreaId, apesar de todos existirem como
        // FKs "lógicas" na entidade.
        [Fact]
        public void Configure_ShouldNotDeclareAnyForeignKey()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
