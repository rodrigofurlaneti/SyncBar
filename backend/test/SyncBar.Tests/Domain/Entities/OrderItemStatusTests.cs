using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class OrderItemStatusTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string name = "Preparando";

            // Act
            var result = OrderItemStatus.Create(name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = OrderItemStatus.Create(invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItemStatus.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var status = OrderItemStatus.Create("Pronto").Value;

            // Act
            status.Touch();

            // Assert
            status.UpdatedAt.Should().NotBeNull();
            status.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var status = OrderItemStatus.Create("Cancelado").Value;

            // Act
            status.Deactivate();

            // Assert
            status.IsActive.Should().BeFalse();
            status.UpdatedAt.Should().NotBeNull();
            status.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OrderItemStatus), true) as OrderItemStatus;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
