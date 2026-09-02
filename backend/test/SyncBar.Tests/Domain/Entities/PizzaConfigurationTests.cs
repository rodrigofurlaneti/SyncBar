using System.Linq;
using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PizzaConfigurationTests
    {
        [Fact]
        public void Create_WithValidProductId_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long productId = 5;

            // Act
            var result = PizzaConfiguration.Create(productId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var config = result.Value;
            config.Should().NotBeNull();
            config.ProductId.Should().Be(productId);
            config.IsActive.Should().BeTrue();
            config.Sizes.Should().BeEmpty();
            config.Crusts.Should().BeEmpty();
            config.Edges.Should().BeEmpty();
            config.FlavorPrices.Should().BeEmpty();
            config.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            config.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void AddSize_WithValidArguments_ShouldAddSizeAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("Grande", 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.Sizes.Should().HaveCount(1);
            var size = result.Value;
            size.Name.Should().Be("Grande");
            size.Slices.Should().Be(8);
            size.AcceptedFractions.Should().Be(2);
            size.DisplayOrder.Should().Be(1);
            size.IsActive.Should().BeTrue();
            config.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void AddSize_WithDuplicateActiveName_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            config.AddSize("Grande", 8, 2, 1);

            // Act
            var result = config.AddSize("grande", 6, 1, 2);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.DuplicateSizeName");
            result.Error.Message.Should().Be("A size with this name already exists.");
            config.Sizes.Should().HaveCount(1);
        }

        [Fact]
        public void AddSize_WithEmptyName_ShouldReturnFailureResultFromPizzaSize()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("   ", 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.EmptyName");
        }

        [Fact]
        public void AddSize_WithInvalidAcceptedFractions_ShouldReturnFailureResultFromPizzaSize()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("Grande", 8, 5, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.InvalidAcceptedFractions");
        }

        [Fact]
        public void UpdateSize_WithValidArguments_ShouldUpdateSizeAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.UpdateSize(size.Id, "Broto", 4, 1, 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            size.Name.Should().Be("Broto");
            size.Slices.Should().Be(4);
            size.AcceptedFractions.Should().Be(1);
            size.DisplayOrder.Should().Be(2);
        }

        [Fact]
        public void UpdateSize_WithNonExistentId_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.UpdateSize(999, "Broto", 4, 1, 2);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.SizeNotFound");
            result.Error.Message.Should().Be("Size not found.");
        }

        [Fact]
        public void UpdateSize_WithInvalidDetails_ShouldReturnFailureResultFromPizzaSize()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.UpdateSize(size.Id, "", 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.EmptyName");
        }

        [Fact]
        public void RemoveSize_WithExistingId_ShouldDeactivateSizeAndRelatedFlavorPrices()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(10, size.Id, 30m);

            // Act
            var result = config.RemoveSize(size.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            size.IsActive.Should().BeFalse();
            config.FlavorPrices.Single(p => p.PizzaSizeId == size.Id).IsActive.Should().BeFalse();
        }

        [Fact]
        public void RemoveSize_WithNonExistentId_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.RemoveSize(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.SizeNotFound");
        }

        [Fact]
        public void AddCrust_WithValidArguments_ShouldAddCrustAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddCrust("Borda Recheada", 8m, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.Crusts.Should().HaveCount(1);
            result.Value.Name.Should().Be("Borda Recheada");
            result.Value.ExtraPrice.Should().Be(8m);
            config.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void AddCrust_WithNegativeExtraPrice_ShouldReturnFailureResultFromPizzaCrust()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddCrust("Borda", -1m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaCrust.InvalidExtraPrice");
        }

        [Fact]
        public void RemoveCrust_WithExistingId_ShouldDeactivateCrust()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var crust = config.AddCrust("Borda Recheada", 8m, 1).Value;

            // Act
            var result = config.RemoveCrust(crust.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            crust.IsActive.Should().BeFalse();
        }

        [Fact]
        public void RemoveCrust_WithNonExistentId_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.RemoveCrust(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.CrustNotFound");
            result.Error.Message.Should().Be("Crust not found.");
        }

        [Fact]
        public void AddEdge_WithValidArguments_ShouldAddEdgeAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddEdge("Catupiry", 6m, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.Edges.Should().HaveCount(1);
            result.Value.Name.Should().Be("Catupiry");
            result.Value.ExtraPrice.Should().Be(6m);
            config.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void AddEdge_WithNegativeExtraPrice_ShouldReturnFailureResultFromPizzaEdge()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddEdge("Catupiry", -1m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaEdge.InvalidExtraPrice");
        }

        [Fact]
        public void RemoveEdge_WithExistingId_ShouldDeactivateEdge()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var edge = config.AddEdge("Catupiry", 6m, 1).Value;

            // Act
            var result = config.RemoveEdge(edge.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            edge.IsActive.Should().BeFalse();
        }

        [Fact]
        public void RemoveEdge_WithNonExistentId_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.RemoveEdge(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.EdgeNotFound");
            result.Error.Message.Should().Be("Edge not found.");
        }

        [Fact]
        public void SetFlavorPrice_WhenSizeDoesNotExist_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.SetFlavorPrice(10, 999, 30m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.SizeNotFound");
        }

        [Fact]
        public void SetFlavorPrice_WhenNoExistingPrice_ShouldCreateFlavorPrice()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.SetFlavorPrice(10, size.Id, 30m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.FlavorPrices.Should().HaveCount(1);
            result.Value.Price.Should().Be(30m);
            result.Value.PizzaFlavorId.Should().Be(10);
            result.Value.PizzaSizeId.Should().Be(size.Id);
        }

        [Fact]
        public void SetFlavorPrice_WhenPriceAlreadyExists_ShouldUpdateExistingFlavorPrice()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(10, size.Id, 30m);

            // Act
            var result = config.SetFlavorPrice(10, size.Id, 45m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.FlavorPrices.Should().HaveCount(1);
            result.Value.Price.Should().Be(45m);
        }

        [Fact]
        public void SetFlavorPrice_WithNegativePrice_ShouldReturnFailureResultFromPizzaFlavorPrice()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.SetFlavorPrice(10, size.Id, -5m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaFlavorPrice.InvalidPrice");
        }

        [Fact]
        public void RemoveFlavor_WithExistingFlavor_ShouldDeactivateOnlyThatFlavorsPrices()
        {
            // Arrange
            // Note: PizzaSize instances created via the aggregate's factory always have Id == 0
            // (never persisted by EF in this test), so a single size is used here to avoid two
            // same-type children colliding on Id — see task guidance on this known limitation.
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(10, size.Id, 30m);
            config.SetFlavorPrice(11, size.Id, 20m);

            // Act
            var result = config.RemoveFlavor(10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            config.FlavorPrices.Single(p => p.PizzaFlavorId == 10).IsActive.Should().BeFalse();
            config.FlavorPrices.Single(p => p.PizzaFlavorId == 11).IsActive.Should().BeTrue();
        }

        [Fact]
        public void RemoveFlavor_WithNoPricesSet_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.RemoveFlavor(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.FlavorNotFound");
            result.Error.Message.Should().Be("This flavor has no price set on this pizza.");
        }

        [Fact]
        public void CalculateUnitPrice_WithSingleFlavorAndNoCrustOrEdge_ShouldReturnFlavorPrice()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, null, [1]);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(30m);
        }

        [Fact]
        public void CalculateUnitPrice_WithMultipleFlavors_ShouldReturnMostExpensiveFlavorPrice()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);
            config.SetFlavorPrice(2, size.Id, 40m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, null, [1, 2]);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(40m);
        }

        [Fact]
        public void CalculateUnitPrice_WithCrustAndEdge_ShouldSumAllExtras()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            var crust = config.AddCrust("Borda Recheada", 8m, 1).Value;
            var edge = config.AddEdge("Catupiry", 6m, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);
            config.SetFlavorPrice(2, size.Id, 40m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, crust.Id, edge.Id, [1, 2]);

            // Assert
            // 40 (max flavor) + 8 (crust) + 6 (edge) = 54
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(54m);
        }

        [Fact]
        public void CalculateUnitPrice_WithNonExistentSize_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.CalculateUnitPrice(999, null, null, [1]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.SizeNotFound");
        }

        [Fact]
        public void CalculateUnitPrice_WithNoFlavorsSelected_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, null, []);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.NoFlavorsSelected");
            result.Error.Message.Should().Be("At least one flavor must be selected.");
        }

        [Fact]
        public void CalculateUnitPrice_WithDuplicateFlavorSelection_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, null, [1, 1]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.DuplicateFlavorSelection");
        }

        [Fact]
        public void CalculateUnitPrice_WithMoreFlavorsThanAcceptedFractions_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Broto", 4, 1, 1).Value; // accepts only 1 fraction
            config.SetFlavorPrice(1, size.Id, 20m);
            config.SetFlavorPrice(2, size.Id, 25m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, null, [1, 2]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.TooManyFractions");
        }

        [Fact]
        public void CalculateUnitPrice_WithFlavorNotAvailableForSize_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act (no flavor price set for flavor 1 on this size)
            var result = config.CalculateUnitPrice(size.Id, null, null, [1]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.FlavorNotAvailableForSize");
        }

        [Fact]
        public void CalculateUnitPrice_WithNonExistentCrust_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, 999, null, [1]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.CrustNotFound");
        }

        [Fact]
        public void CalculateUnitPrice_WithNonExistentEdge_ShouldReturnFailureResult()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;
            config.SetFlavorPrice(1, size.Id, 30m);

            // Act
            var result = config.CalculateUnitPrice(size.Id, null, 999, [1]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaConfiguration.EdgeNotFound");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            config.Deactivate();

            // Assert
            config.IsActive.Should().BeFalse();
            config.UpdatedAt.Should().NotBeNull();
            config.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaConfiguration), true) as PizzaConfiguration;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
