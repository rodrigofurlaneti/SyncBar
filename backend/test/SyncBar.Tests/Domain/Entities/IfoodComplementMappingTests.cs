using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // IfoodComplementMapping.Create has no validation branches (always succeeds) and the entity
    // exposes no other public state-changing methods (no Deactivate/Touch present in the source),
    // so this file covers the happy path, the generated GUIDs, and the private constructor only.
    public class IfoodComplementMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long complementId = 10;
            long branchId = 1;

            // Act
            var result = IfoodComplementMapping.Create(complementId, branchId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.ComplementId.Should().Be(complementId);
            mapping.BranchId.Should().Be(branchId);
            mapping.IfoodOptionId.Should().NotBe(Guid.Empty);
            mapping.IfoodProductId.Should().NotBe(Guid.Empty);
            mapping.IfoodOptionId.Should().NotBe(mapping.IfoodProductId);
            mapping.IsActive.Should().BeTrue();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_CalledTwice_ShouldGenerateDistinctGuidsPerMapping()
        {
            // Act
            var first = IfoodComplementMapping.Create(10, 1).Value;
            var second = IfoodComplementMapping.Create(10, 1).Value;

            // Assert
            first.IfoodOptionId.Should().NotBe(second.IfoodOptionId);
            first.IfoodProductId.Should().NotBe(second.IfoodProductId);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodComplementMapping), true) as IfoodComplementMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
