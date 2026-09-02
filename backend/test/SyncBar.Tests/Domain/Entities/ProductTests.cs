using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ProductTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            long categoryId = 2;
            long unitOfMeasureId = 3;
            string name = "Cerveja Long Neck";
            string description = "Cerveja gelada 355ml";
            string barcode = "7891234567890";
            decimal salePrice = 12.90m;
            decimal? costPrice = 6.50m;
            bool isStockControlled = true;
            int? preparationTimeMinutes = null;

            // Act
            var result = Product.Create(companyId, categoryId, unitOfMeasureId, name, description, barcode, salePrice, costPrice, isStockControlled, preparationTimeMinutes);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var product = result.Value;
            product.Should().NotBeNull();
            product.CompanyId.Should().Be(companyId);
            product.CategoryId.Should().Be(categoryId);
            product.UnitOfMeasureId.Should().Be(unitOfMeasureId);
            product.Name.Should().Be(name);
            product.Description.Should().Be(description);
            product.Barcode.Should().Be(barcode);
            product.SalePrice.Should().Be(salePrice);
            product.CostPrice.Should().Be(costPrice);
            product.IsStockControlled.Should().Be(isStockControlled);
            product.PreparationTimeMinutes.Should().Be(preparationTimeMinutes);
            product.ImageUrl.Should().BeNull();
            product.IsActive.Should().BeTrue();
            product.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            product.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Product.Create(1, 2, 3, invalidName, null, null, 10m, null, false, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Product.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            var result = product.UpdateDetails(20, 30, "Cerveja Puro Malte", "Nova descrição", "111222333", 15.5m, 8m, true, 5);

            // Assert
            result.IsSuccess.Should().BeTrue();
            product.CategoryId.Should().Be(20);
            product.UnitOfMeasureId.Should().Be(30);
            product.Name.Should().Be("Cerveja Puro Malte");
            product.Description.Should().Be("Nova descrição");
            product.Barcode.Should().Be("111222333");
            product.SalePrice.Should().Be(15.5m);
            product.CostPrice.Should().Be(8m);
            product.IsStockControlled.Should().BeTrue();
            product.PreparationTimeMinutes.Should().Be(5);
            product.UpdatedAt.Should().NotBeNull();
            product.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            var result = product.UpdateDetails(2, 3, invalidName, null, null, 10m, null, false, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Product.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
            product.Name.Should().Be("Cerveja");
        }

        [Fact]
        public void UpdateDetails_WithNegativeSalePrice_ShouldReturnFailureResult()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            var result = product.UpdateDetails(2, 3, "Cerveja", null, null, -1m, null, false, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Product.InvalidSalePrice");
            result.Error.Message.Should().Be("Sale price cannot be negative.");
            product.SalePrice.Should().Be(10m);
        }

        [Fact]
        public void SetImage_ShouldUpdateImageUrlAndSetUpdatedAt()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            product.SetImage("https://example.com/cerveja.png");

            // Assert
            product.ImageUrl.Should().Be("https://example.com/cerveja.png");
            product.UpdatedAt.Should().NotBeNull();
            product.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            product.Touch();

            // Assert
            product.UpdatedAt.Should().NotBeNull();
            product.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;

            // Act
            product.Deactivate();

            // Assert
            product.IsActive.Should().BeFalse();
            product.UpdatedAt.Should().NotBeNull();
            product.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Activate_ShouldUpdateIsActiveToTrueAndSetUpdatedAt()
        {
            // Arrange
            var product = Product.Create(1, 2, 3, "Cerveja", null, null, 10m, null, false, null).Value;
            product.Deactivate();

            // Act
            product.Activate();

            // Assert
            product.IsActive.Should().BeTrue();
            product.UpdatedAt.Should().NotBeNull();
            product.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Product), true) as Product;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
