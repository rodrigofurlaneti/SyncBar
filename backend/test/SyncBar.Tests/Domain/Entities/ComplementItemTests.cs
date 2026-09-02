using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ComplementItemTests
    {
        [Fact]
        public void Create_WithValidArgumentsAndNoLinkedProduct_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Sem cebola";

            // Act
            var result = ComplementItem.Create(companyId, name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var item = result.Value;
            item.Should().NotBeNull();
            item.CompanyId.Should().Be(companyId);
            item.Name.Should().Be(name);
            item.LinkedProductId.Should().BeNull();
            item.IsActive.Should().BeTrue();
            item.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            item.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithLinkedProductId_ShouldReturnSuccessResultWithLinkedProductSet()
        {
            // Arrange & Act
            var result = ComplementItem.Create(1, "X-Salada", 99);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.LinkedProductId.Should().Be(99);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = ComplementItem.Create(1, invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementItem.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateName_WithValidName_ShouldUpdateNameAndSetUpdatedAt()
        {
            // Arrange
            var item = ComplementItem.Create(1, "Bacon extra").Value;

            // Act
            var result = item.UpdateName("Bacon extra crocante");

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Name.Should().Be("Bacon extra crocante");
            item.UpdatedAt.Should().NotBeNull();
            item.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateName_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var item = ComplementItem.Create(1, "Bacon extra").Value;

            // Act
            var result = item.UpdateName(invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementItem.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void LinkToProduct_WithProductId_ShouldSetLinkedProductIdAndUpdatedAt()
        {
            // Arrange
            var item = ComplementItem.Create(1, "X-Salada").Value;

            // Act
            item.LinkToProduct(77);

            // Assert
            item.LinkedProductId.Should().Be(77);
            item.UpdatedAt.Should().NotBeNull();
            item.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void LinkToProduct_WithNull_ShouldUnlinkProduct()
        {
            // Arrange
            var item = ComplementItem.Create(1, "X-Salada", 77).Value;

            // Act
            item.LinkToProduct(null);

            // Assert
            item.LinkedProductId.Should().BeNull();
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var item = ComplementItem.Create(1, "Sem cebola").Value;

            // Act
            item.Deactivate();

            // Assert
            item.IsActive.Should().BeFalse();
            item.UpdatedAt.Should().NotBeNull();
            item.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ComplementItem), true) as ComplementItem;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
