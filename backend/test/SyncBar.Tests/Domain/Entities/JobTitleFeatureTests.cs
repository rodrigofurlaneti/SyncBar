using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class JobTitleFeatureTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long jobTitleId = 1;
            long appFeatureId = 2;

            // Act
            var result = JobTitleFeature.Create(jobTitleId, appFeatureId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.JobTitleId.Should().Be(jobTitleId);
            result.Value.AppFeatureId.Should().Be(appFeatureId);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public void Create_WithInvalidIds_ShouldReturnFailureResult(long jobTitleId, long appFeatureId)
        {
            // Act
            var result = JobTitleFeature.Create(jobTitleId, appFeatureId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("JobTitleFeature.InvalidIds");
            result.Error.Message.Should().Be("Ids must be greater than zero.");
        }

        [Fact]
        public void Reactivate_ShouldSetIsActiveTrueAndUpdatedAt()
        {
            // Arrange
            var jobTitleFeature = JobTitleFeature.Create(1, 2).Value;
            jobTitleFeature.Deactivate();

            // Act
            jobTitleFeature.Reactivate();

            // Assert
            jobTitleFeature.IsActive.Should().BeTrue();
            jobTitleFeature.UpdatedAt.Should().NotBeNull();
            jobTitleFeature.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var jobTitleFeature = JobTitleFeature.Create(1, 2).Value;

            // Act
            jobTitleFeature.Deactivate();

            // Assert
            jobTitleFeature.IsActive.Should().BeFalse();
            jobTitleFeature.UpdatedAt.Should().NotBeNull();
            jobTitleFeature.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(JobTitleFeature), true) as JobTitleFeature;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
