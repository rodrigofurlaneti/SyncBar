using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class JobTitleTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Garçom";

            // Act
            var result = JobTitle.Create(companyId, name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var jobTitle = result.Value;
            jobTitle.Should().NotBeNull();
            jobTitle.CompanyId.Should().Be(companyId);
            jobTitle.Name.Should().Be(name);
            jobTitle.IsActive.Should().BeTrue();
            jobTitle.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            jobTitle.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = JobTitle.Create(1, invalidName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("JobTitle.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var jobTitle = JobTitle.Create(1, "Garçom").Value;

            // Act
            jobTitle.Touch();

            // Assert
            jobTitle.UpdatedAt.Should().NotBeNull();
            jobTitle.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var jobTitle = JobTitle.Create(1, "Garçom").Value;

            // Act
            jobTitle.Deactivate();

            // Assert
            jobTitle.IsActive.Should().BeFalse();
            jobTitle.UpdatedAt.Should().NotBeNull();
            jobTitle.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(JobTitle), true) as JobTitle;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
