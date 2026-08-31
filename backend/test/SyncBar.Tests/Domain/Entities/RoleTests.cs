using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class RoleTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Manager";
            string description = "Has access to managerial features.";

            // Act
            var result = Role.Create(companyId, name, description);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CompanyId.Should().Be(companyId);
            result.Value.Name.Should().Be(name);
            result.Value.Description.Should().Be(description);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string invalidName)
        {
            // Act
            var result = Role.Create(1, invalidName, "Description test");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Role.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var role = Role.Create(1, "Cashier", null).Value;

            // Act
            role.Touch();

            // Assert
            role.UpdatedAt.Should().NotBeNull();
            role.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var role = Role.Create(1, "Waiter", "Attends tables").Value;

            // Act
            role.Deactivate();

            // Assert
            role.IsActive.Should().BeFalse();
            role.UpdatedAt.Should().NotBeNull();
            role.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Role), true) as Role;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
