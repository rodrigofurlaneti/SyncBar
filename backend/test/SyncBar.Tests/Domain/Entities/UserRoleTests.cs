using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class UserRoleTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 10;
            long appUserId = 100;
            long roleId = 5;

            // Act
            var result = UserRole.Create(companyId, appUserId, roleId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CompanyId.Should().Be(companyId);
            result.Value.AppUserId.Should().Be(appUserId);
            result.Value.RoleId.Should().Be(roleId);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var userRole = UserRole.Create(1, 2, 3).Value;

            // Act
            userRole.Touch();

            // Assert
            userRole.UpdatedAt.Should().NotBeNull();
            userRole.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var userRole = UserRole.Create(1, 2, 3).Value;

            // Act
            userRole.Deactivate();

            // Assert
            userRole.IsActive.Should().BeFalse();
            userRole.UpdatedAt.Should().NotBeNull();
            userRole.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(UserRole), true) as UserRole;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
