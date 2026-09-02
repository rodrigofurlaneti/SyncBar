using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class DiningAreaTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            string name = "Salão Principal";

            // Act
            var result = DiningArea.Create(branchId, name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var diningArea = result.Value;
            diningArea.Should().NotBeNull();
            diningArea.BranchId.Should().Be(branchId);
            diningArea.Name.Should().Be(name);
            diningArea.IsActive.Should().BeTrue();
            diningArea.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            diningArea.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidBranchId_ShouldReturnFailureResult(long invalidBranchId)
        {
            // Act
            var result = DiningArea.Create(invalidBranchId, "Salão");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningArea.InvalidBranchId");
            result.Error.Message.Should().Be("BranchId is required and must be greater than zero.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = DiningArea.Create(1, invalidName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningArea.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateName_WithValidName_ShouldUpdateNameAndSetUpdatedAt()
        {
            // Arrange
            var diningArea = DiningArea.Create(1, "Salão Principal").Value;

            // Act
            diningArea.UpdateName("Terraço");

            // Assert
            diningArea.Name.Should().Be("Terraço");
            diningArea.UpdatedAt.Should().NotBeNull();
            diningArea.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateName_WithEmptyOrWhitespaceName_ShouldNotChangeNameOrUpdatedAt(string? invalidName)
        {
            // Arrange
            var diningArea = DiningArea.Create(1, "Salão Principal").Value;

            // Act
            diningArea.UpdateName(invalidName);

            // Assert
            diningArea.Name.Should().Be("Salão Principal");
            diningArea.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var diningArea = DiningArea.Create(1, "Salão Principal").Value;

            // Act
            diningArea.Touch();

            // Assert
            diningArea.UpdatedAt.Should().NotBeNull();
            diningArea.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var diningArea = DiningArea.Create(1, "Salão Principal").Value;

            // Act
            diningArea.Deactivate();

            // Assert
            diningArea.IsActive.Should().BeFalse();
            diningArea.UpdatedAt.Should().NotBeNull();
            diningArea.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(DiningArea), true) as DiningArea;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
