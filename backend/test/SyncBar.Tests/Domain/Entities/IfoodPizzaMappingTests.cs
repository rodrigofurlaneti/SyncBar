using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodPizzaMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long pizzaConfigurationId = 1;
            long branchId = 2;
            string ifoodPizzaId = "ifood-pizza-1";

            // Act
            var result = IfoodPizzaMapping.Create(pizzaConfigurationId, branchId, ifoodPizzaId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.PizzaConfigurationId.Should().Be(pizzaConfigurationId);
            mapping.BranchId.Should().Be(branchId);
            mapping.IfoodPizzaId.Should().Be(ifoodPizzaId);
            mapping.IsActive.Should().BeTrue();
            mapping.Elements.Should().BeEmpty();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceIfoodPizzaId_ShouldReturnFailureResult(string? invalidIfoodPizzaId)
        {
            // Act
            var result = IfoodPizzaMapping.Create(1, 2, invalidIfoodPizzaId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodPizzaMapping.EmptyId");
            result.Error.Message.Should().Be("Ifood pizza id is required.");
        }

        [Fact]
        public void SetElement_WhenNoExistingElement_ShouldCreateAndAddNewElement()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "ifood-pizza-1").Value;

            // Act
            var element = mapping.SetElement(IfoodPizzaElementKind.Size, 100, "ifood-size-1");

            // Assert
            element.Should().NotBeNull();
            element.Kind.Should().Be(IfoodPizzaElementKind.Size);
            element.LocalId.Should().Be(100);
            element.IfoodElementId.Should().Be("ifood-size-1");
            mapping.Elements.Should().HaveCount(1);
            mapping.Elements.Should().Contain(element);
            mapping.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetElement_WhenActiveElementExistsForKindAndLocalId_ShouldUpdateExistingElementInstead()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "ifood-pizza-1").Value;
            var original = mapping.SetElement(IfoodPizzaElementKind.Topping, 200, "ifood-topping-old");

            // Act
            var updated = mapping.SetElement(IfoodPizzaElementKind.Topping, 200, "ifood-topping-new");

            // Assert
            updated.Should().BeSameAs(original);
            updated.IfoodElementId.Should().Be("ifood-topping-new");
            mapping.Elements.Should().HaveCount(1);
        }

        [Fact]
        public void FindIfoodElementId_WhenElementExists_ShouldReturnItsId()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "ifood-pizza-1").Value;
            mapping.SetElement(IfoodPizzaElementKind.Edge, 300, "ifood-edge-1");

            // Act
            var found = mapping.FindIfoodElementId(IfoodPizzaElementKind.Edge, 300);

            // Assert
            found.Should().Be("ifood-edge-1");
        }

        [Fact]
        public void FindIfoodElementId_WhenElementDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "ifood-pizza-1").Value;

            // Act
            var found = mapping.FindIfoodElementId(IfoodPizzaElementKind.Crust, 999);

            // Assert
            found.Should().BeNull();
        }

        [Fact]
        public void UpdateIfoodPizzaId_ShouldUpdateIdAndSetUpdatedAt()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "old-pizza-id").Value;

            // Act
            mapping.UpdateIfoodPizzaId("new-pizza-id");

            // Assert
            mapping.IfoodPizzaId.Should().Be("new-pizza-id");
            mapping.UpdatedAt.Should().NotBeNull();
            mapping.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var mapping = IfoodPizzaMapping.Create(1, 2, "ifood-pizza-1").Value;

            // Act
            mapping.Deactivate();

            // Assert
            mapping.IsActive.Should().BeFalse();
            mapping.UpdatedAt.Should().NotBeNull();
            mapping.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodPizzaMapping), true) as IfoodPizzaMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
