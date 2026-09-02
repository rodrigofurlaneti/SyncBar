using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AppUserFeatureTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long appUserId = 1;
            long appFeatureId = 2;

            // Act
            var result = AppUserFeature.Create(appUserId, appFeatureId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var feature = result.Value;
            feature.Should().NotBeNull();
            feature.AppUserId.Should().Be(appUserId);
            feature.AppFeatureId.Should().Be(appFeatureId);
            feature.IsActive.Should().BeTrue();
            feature.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            feature.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public void Create_WithInvalidIds_ShouldReturnFailureResult(long appUserId, long appFeatureId)
        {
            // Act
            var result = AppUserFeature.Create(appUserId, appFeatureId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppUserFeature.InvalidIds");
            result.Error.Message.Should().Be("Ids must be greater than zero.");
        }

        [Fact]
        public void Reactivate_ShouldUpdateIsActiveToTrueAndSetUpdatedAt()
        {
            // Arrange
            var feature = AppUserFeature.Create(1, 2).Value;
            feature.Deactivate();

            // Act
            feature.Reactivate();

            // Assert
            feature.IsActive.Should().BeTrue();
            feature.UpdatedAt.Should().NotBeNull();
            feature.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var feature = AppUserFeature.Create(1, 2).Value;

            // Act
            feature.Deactivate();

            // Assert
            feature.IsActive.Should().BeFalse();
            feature.UpdatedAt.Should().NotBeNull();
            feature.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(AppUserFeature), true) as AppUserFeature;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
