using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class LogTrackerConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(LogTracker))!;

        [Fact]
        public void Configure_ShouldMapToLogTrackerTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("logtracker");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(LogTracker.Id));
            EntityType.FindProperty(nameof(LogTracker.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_DirectoryNameClassNameAndMethodName_ShouldHaveMaxLength150()
        {
            var directoryName = EntityType.FindProperty(nameof(LogTracker.DirectoryName))!;
            var className = EntityType.FindProperty(nameof(LogTracker.ClassName))!;
            var methodName = EntityType.FindProperty(nameof(LogTracker.MethodName))!;

            directoryName.IsNullable.Should().BeTrue();
            directoryName.GetColumnType().Should().Be("varchar(150)");

            className.IsNullable.Should().BeFalse();
            className.GetColumnType().Should().Be("varchar(150)");

            methodName.IsNullable.Should().BeFalse();
            methodName.GetColumnType().Should().Be("varchar(150)");
        }

        [Fact]
        public void Configure_IsSuccess_ShouldBeRequiredTinyint_ExecutionTimeMs_ShouldBeNullableBigint()
        {
            var isSuccess = EntityType.FindProperty(nameof(LogTracker.IsSuccess))!;
            var executionTimeMs = EntityType.FindProperty(nameof(LogTracker.ExecutionTimeMs))!;

            isSuccess.IsNullable.Should().BeFalse();
            isSuccess.GetColumnType().Should().Be("tinyint(1)");

            executionTimeMs.IsNullable.Should().BeTrue();
            executionTimeMs.GetColumnType().Should().Be("bigint");
        }

        [Fact]
        public void Configure_MessageErrorMessageAndStackTrace_ShouldBeOptionalText()
        {
            EntityType.FindProperty(nameof(LogTracker.Message))!.GetColumnType().Should().Be("text");
            EntityType.FindProperty(nameof(LogTracker.ErrorMessage))!.GetColumnType().Should().Be("text");
            EntityType.FindProperty(nameof(LogTracker.StackTrace))!.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_IpAddress_ShouldBeOptionalVarchar45()
        {
            EntityType.FindProperty(nameof(LogTracker.IpAddress))!.GetColumnType().Should().Be("varchar(45)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(LogTracker.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(LogTracker.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(LogTracker.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnAppUserIdAndCreatedAt()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_LogTracker_AppUserId" && i.Properties.Single().Name == nameof(LogTracker.AppUserId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_LogTracker_CreatedAt" && i.Properties.Single().Name == nameof(LogTracker.CreatedAt));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToAppUser()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_LogTracker_AppUser" &&
                fk.PrincipalEntityType.ClrType == typeof(AppUser) &&
                fk.Properties.Single().Name == nameof(LogTracker.AppUserId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
