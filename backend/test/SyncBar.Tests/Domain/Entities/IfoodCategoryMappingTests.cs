using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // IfoodCategoryMapping só expõe Create() — não há Touch()/Update()/Deactivate() no código
    // real, então não há testes para métodos inexistentes.
    public class IfoodCategoryMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long categoryId = 1;
            long branchId = 2;
            string ifoodCategoryId = "IFOOD-CAT-123";

            // Act
            var result = IfoodCategoryMapping.Create(categoryId, branchId, ifoodCategoryId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.CategoryId.Should().Be(categoryId);
            mapping.BranchId.Should().Be(branchId);
            mapping.IfoodCategoryId.Should().Be(ifoodCategoryId);
            mapping.IsActive.Should().BeTrue();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceIfoodCategoryId_ShouldReturnFailureResult(string? invalidIfoodCategoryId)
        {
            // Act
            var result = IfoodCategoryMapping.Create(1, 2, invalidIfoodCategoryId!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodCategoryMapping.EmptyId");
            result.Error.Message.Should().Be("Ifood category id is required.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodCategoryMapping), true) as IfoodCategoryMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
