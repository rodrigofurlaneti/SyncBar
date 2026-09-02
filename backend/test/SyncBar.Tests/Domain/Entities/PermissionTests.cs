using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PermissionTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string code = "MENU_EDIT";
            string name = "Edit Menu";
            string moduleName = "Menu";

            // Act
            var result = Permission.Create(code, name, moduleName);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Code.Should().Be(code);
            result.Value.Name.Should().Be(name);
            result.Value.ModuleName.Should().Be(moduleName);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCode_ShouldReturnFailureResult(string? invalidCode)
        {
            // Act
            var result = Permission.Create(invalidCode, "Edit Menu", "Menu");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Permission.EmptyCode");
            result.Error.Message.Should().Be("Code is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Permission.Create("MENU_EDIT", invalidName, "Menu");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Permission.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceModuleName_ShouldReturnFailureResult(string? invalidModuleName)
        {
            // Act
            var result = Permission.Create("MENU_EDIT", "Edit Menu", invalidModuleName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Permission.EmptyModuleName");
            result.Error.Message.Should().Be("ModuleName is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var permission = Permission.Create("MENU_EDIT", "Edit Menu", "Menu").Value;

            // Act
            permission.Touch();

            // Assert
            permission.UpdatedAt.Should().NotBeNull();
            permission.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var permission = Permission.Create("MENU_EDIT", "Edit Menu", "Menu").Value;

            // Act
            permission.Deactivate();

            // Assert
            permission.IsActive.Should().BeFalse();
            permission.UpdatedAt.Should().NotBeNull();
            permission.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Permission), true) as Permission;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
