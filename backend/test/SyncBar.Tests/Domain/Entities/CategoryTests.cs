using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CategoryTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Bebidas";
            int displayOrder = 2;

            // Act
            var result = Category.Create(companyId, name, displayOrder);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var category = result.Value;
            category.Should().NotBeNull();
            category.CompanyId.Should().Be(companyId);
            category.Name.Should().Be(name);
            category.DisplayOrder.Should().Be(displayOrder);
            category.IsActive.Should().BeTrue();
            category.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            category.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Category.Create(1, invalidName!, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Category.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdateNameDisplayOrderAndSetUpdatedAt()
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;

            // Act
            var result = category.UpdateDetails("Petiscos", 3);

            // Assert
            result.IsSuccess.Should().BeTrue();
            category.Name.Should().Be("Petiscos");
            category.DisplayOrder.Should().Be(3);
            category.UpdatedAt.Should().NotBeNull();
            category.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;

            // Act
            var result = category.UpdateDetails(invalidName!, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Category.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithNegativeDisplayOrder_ShouldReturnFailureResult()
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;

            // Act
            var result = category.UpdateDetails("Bebidas", -1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Category.InvalidDisplayOrder");
            result.Error.Message.Should().Be("Display order cannot be negative.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;

            // Act
            category.Deactivate();

            // Assert
            category.IsActive.Should().BeFalse();
            category.UpdatedAt.Should().NotBeNull();
            category.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Activate_ShouldUpdateIsActiveToTrueAndSetUpdatedAt()
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;
            category.Deactivate();

            // Act
            category.Activate();

            // Assert
            category.IsActive.Should().BeTrue();
            category.UpdatedAt.Should().NotBeNull();
            category.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var category = Category.Create(1, "Bebidas", 0).Value;

            // Act
            category.Touch();

            // Assert
            category.UpdatedAt.Should().NotBeNull();
            category.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Category), true) as Category;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
