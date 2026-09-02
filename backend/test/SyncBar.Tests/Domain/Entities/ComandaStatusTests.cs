using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ComandaStatusTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string name = "Em uso";

            // Act
            var result = ComandaStatus.Create(name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = ComandaStatus.Create(invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaStatus.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var comandaStatus = ComandaStatus.Create("Bloqueada").Value;

            // Act
            comandaStatus.Touch();

            // Assert
            comandaStatus.UpdatedAt.Should().NotBeNull();
            comandaStatus.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var comandaStatus = ComandaStatus.Create("Extraviada").Value;

            // Act
            comandaStatus.Deactivate();

            // Assert
            comandaStatus.IsActive.Should().BeFalse();
            comandaStatus.UpdatedAt.Should().NotBeNull();
            comandaStatus.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ComandaStatus), true) as ComandaStatus;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
