using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // IfoodComplementGroupMapping.Create has no validation branches in the source (it always
    // returns success) — only the happy path and construction-related behavior are testable here.
    public class IfoodComplementGroupMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long complementGroupId = 5;
            long branchId = 1;

            // Act
            var result = IfoodComplementGroupMapping.Create(complementGroupId, branchId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.ComplementGroupId.Should().Be(complementGroupId);
            mapping.BranchId.Should().Be(branchId);
            mapping.IfoodOptionGroupId.Should().NotBe(Guid.Empty);
            mapping.IsActive.Should().BeTrue();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_CalledTwice_ShouldGenerateDifferentIfoodOptionGroupIds()
        {
            // Act
            var first = IfoodComplementGroupMapping.Create(5, 1).Value;
            var second = IfoodComplementGroupMapping.Create(5, 1).Value;

            // Assert
            first.IfoodOptionGroupId.Should().NotBe(second.IfoodOptionGroupId);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodComplementGroupMapping), true) as IfoodComplementGroupMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
