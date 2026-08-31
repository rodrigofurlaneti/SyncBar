using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class RolePermissionTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long roleId = 10;
            long permissionId = 25;

            // Act
            var result = RolePermission.Create(roleId, permissionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.RoleId.Should().Be(roleId);
            result.Value.PermissionId.Should().Be(permissionId);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var rolePermission = RolePermission.Create(1, 2).Value;

            // Act
            rolePermission.Touch();

            // Assert
            rolePermission.UpdatedAt.Should().NotBeNull();
            rolePermission.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var rolePermission = RolePermission.Create(1, 2).Value;

            // Act
            rolePermission.Deactivate();

            // Assert
            rolePermission.IsActive.Should().BeFalse();
            rolePermission.UpdatedAt.Should().NotBeNull();
            rolePermission.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(RolePermission), true) as RolePermission;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse(); // Valor padrão de booleano não inicializado (ou via base)
        }
    }
}
