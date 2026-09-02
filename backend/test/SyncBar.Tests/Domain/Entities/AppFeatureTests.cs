using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AppFeatureTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string code = "MENU_MANAGEMENT";
            string name = "Menu Management";

            // Act
            var result = AppFeature.Create(code, name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Code.Should().Be(code);
            result.Value.Name.Should().Be(name);
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
            var result = AppFeature.Create(invalidCode, "Valid Name");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppFeature.EmptyCode");
            result.Error.Message.Should().Be("Code is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = AppFeature.Create("VALID_CODE", invalidName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppFeature.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(AppFeature), true) as AppFeature;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
