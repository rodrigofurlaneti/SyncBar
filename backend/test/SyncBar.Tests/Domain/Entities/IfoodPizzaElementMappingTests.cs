using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodPizzaElementMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnInstanceWithCorrectProperties()
        {
            // Arrange
            long ifoodPizzaMappingId = 1;
            byte kind = IfoodPizzaElementKind.Size;
            long localId = 10;
            string ifoodElementId = "ifood-element-abc";

            // Act
            var element = IfoodPizzaElementMapping.Create(ifoodPizzaMappingId, kind, localId, ifoodElementId);

            // Assert
            element.Should().NotBeNull();
            element.IfoodPizzaMappingId.Should().Be(ifoodPizzaMappingId);
            element.Kind.Should().Be(kind);
            element.LocalId.Should().Be(localId);
            element.IfoodElementId.Should().Be(ifoodElementId);
            element.IsActive.Should().BeTrue();
            element.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            element.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void UpdateIfoodElementId_ShouldUpdateIdAndSetUpdatedAt()
        {
            // Arrange
            var element = IfoodPizzaElementMapping.Create(1, IfoodPizzaElementKind.Crust, 10, "old-id");

            // Act
            element.UpdateIfoodElementId("new-id");

            // Assert
            element.IfoodElementId.Should().Be("new-id");
            element.UpdatedAt.Should().NotBeNull();
            element.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodPizzaElementMapping), true) as IfoodPizzaElementMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
