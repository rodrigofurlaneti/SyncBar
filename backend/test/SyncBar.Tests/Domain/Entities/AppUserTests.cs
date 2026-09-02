using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AppUserTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            long? employeeId = 10;
            string userName = "jdoe";
            string email = "jdoe@example.com";
            string passwordHash = "hashed-password";

            // Act
            var result = AppUser.Create(companyId, employeeId, userName, email, passwordHash);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var user = result.Value;
            user.Should().NotBeNull();
            user.CompanyId.Should().Be(companyId);
            user.EmployeeId.Should().Be(employeeId);
            user.UserName.Should().Be(userName);
            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.PasswordSalt.Should().BeNull();
            user.FailedAccessCount.Should().Be(0);
            user.LockoutEndAt.Should().BeNull();
            user.LastLoginAt.Should().BeNull();
            user.IsActive.Should().BeTrue();
            user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceUserName_ShouldReturnFailureResult(string? invalidUserName)
        {
            // Act
            var result = AppUser.Create(1, null, invalidUserName, "jdoe@example.com", "hash");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppUser.EmptyUserName");
            result.Error.Message.Should().Be("UserName is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceEmail_ShouldReturnFailureResult(string? invalidEmail)
        {
            // Act
            var result = AppUser.Create(1, null, "jdoe", invalidEmail, "hash");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppUser.EmptyEmail");
            result.Error.Message.Should().Be("Email is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespacePasswordHash_ShouldReturnFailureResult(string? invalidPasswordHash)
        {
            // Act
            var result = AppUser.Create(1, null, "jdoe", "jdoe@example.com", invalidPasswordHash);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppUser.EmptyPasswordHash");
            result.Error.Message.Should().Be("Password hash is required.");
        }

        [Fact]
        public void IsLockedOut_WhenNoLockoutIsSet_ShouldReturnFalse()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "hash").Value;

            // Act
            var lockedOut = user.IsLockedOut();

            // Assert
            lockedOut.Should().BeFalse();
        }

        [Fact]
        public void RegisterLoginFailure_BelowMaxAttempts_ShouldIncrementCountAndNotLockOut()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "hash").Value;

            // Act
            for (var i = 0; i < 4; i++)
                user.RegisterLoginFailure();

            // Assert
            user.FailedAccessCount.Should().Be(4);
            user.LockoutEndAt.Should().BeNull();
            user.IsLockedOut().Should().BeFalse();
            user.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void RegisterLoginFailure_AtMaxAttempts_ShouldLockOutUser()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "hash").Value;

            // Act
            for (var i = 0; i < 5; i++)
                user.RegisterLoginFailure();

            // Assert
            user.FailedAccessCount.Should().Be(5);
            user.LockoutEndAt.Should().NotBeNull();
            user.LockoutEndAt.Should().BeAfter(DateTime.Now);
            user.LockoutEndAt.Should().BeCloseTo(DateTime.Now.AddMinutes(15), TimeSpan.FromSeconds(2));
            user.IsLockedOut().Should().BeTrue();
        }

        [Fact]
        public void RegisterLoginSuccess_ShouldResetFailedCountClearLockoutAndSetLastLogin()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "hash").Value;
            for (var i = 0; i < 5; i++)
                user.RegisterLoginFailure();
            user.IsLockedOut().Should().BeTrue();

            // Act
            user.RegisterLoginSuccess();

            // Assert
            user.FailedAccessCount.Should().Be(0);
            user.LockoutEndAt.Should().BeNull();
            user.IsLockedOut().Should().BeFalse();
            user.LastLoginAt.Should().NotBeNull();
            user.LastLoginAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void ChangePasswordHash_WithValidHash_ShouldUpdatePasswordHashAndSetUpdatedAt()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "old-hash").Value;

            // Act
            var result = user.ChangePasswordHash("new-hash");

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.PasswordHash.Should().Be("new-hash");
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangePasswordHash_WithEmptyOrWhitespaceHash_ShouldReturnFailureResult(string? invalidHash)
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "old-hash").Value;

            // Act
            var result = user.ChangePasswordHash(invalidHash);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AppUser.EmptyPasswordHash");
            result.Error.Message.Should().Be("Password hash is required.");
            user.PasswordHash.Should().Be("old-hash");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var user = AppUser.Create(1, null, "jdoe", "jdoe@example.com", "hash").Value;

            // Act
            user.Deactivate();

            // Assert
            user.IsActive.Should().BeFalse();
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(AppUser), true) as AppUser;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
