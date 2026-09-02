using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class DiningAreaTableTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long diningAreaId = 1;
            long diningTableId = 5;

            // Act
            var result = DiningAreaTable.Create(diningAreaId, diningTableId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var entity = result.Value;
            entity.Should().NotBeNull();
            entity.DiningAreaId.Should().Be(diningAreaId);
            entity.DiningTableId.Should().Be(diningTableId);
            entity.IsActive.Should().BeTrue();
            entity.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            entity.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidDiningAreaId_ShouldReturnFailureResult(long invalidDiningAreaId)
        {
            // Act
            var result = DiningAreaTable.Create(invalidDiningAreaId, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningAreaTable.InvalidDiningAreaId");
            result.Error.Message.Should().Be("DiningAreaId must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidDiningTableId_ShouldReturnFailureResult(long invalidDiningTableId)
        {
            // Act
            var result = DiningAreaTable.Create(1, invalidDiningTableId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningAreaTable.InvalidDiningTableId");
            result.Error.Message.Should().Be("DiningTableId must be greater than zero.");
        }

        [Fact]
        public void UpdateAssignment_WithValidValues_ShouldUpdateBothIdsAndSetUpdatedAt()
        {
            // Arrange
            var entity = DiningAreaTable.Create(1, 5).Value;

            // Act
            entity.UpdateAssignment(2, 10);

            // Assert
            entity.DiningAreaId.Should().Be(2);
            entity.DiningTableId.Should().Be(10);
            entity.UpdatedAt.Should().NotBeNull();
            entity.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void UpdateAssignment_WithZeroOrNegativeValues_ShouldKeepExistingValuesUnchanged()
        {
            // Arrange
            var entity = DiningAreaTable.Create(1, 5).Value;

            // Act
            entity.UpdateAssignment(0, -1);

            // Assert
            entity.DiningAreaId.Should().Be(1);
            entity.DiningTableId.Should().Be(5);
            // UpdatedAt is still touched even when neither value changes, per implementation.
            entity.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void UpdateAssignment_WithOnlyDiningAreaIdValid_ShouldUpdateOnlyDiningAreaId()
        {
            // Arrange
            var entity = DiningAreaTable.Create(1, 5).Value;

            // Act
            entity.UpdateAssignment(3, 0);

            // Assert
            entity.DiningAreaId.Should().Be(3);
            entity.DiningTableId.Should().Be(5);
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var entity = DiningAreaTable.Create(1, 5).Value;

            // Act
            entity.Touch();

            // Assert
            entity.UpdatedAt.Should().NotBeNull();
            entity.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var entity = DiningAreaTable.Create(1, 5).Value;

            // Act
            entity.Deactivate();

            // Assert
            entity.IsActive.Should().BeFalse();
            entity.UpdatedAt.Should().NotBeNull();
            entity.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(DiningAreaTable), true) as DiningAreaTable;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
