using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class RefreshTokenTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long appUserId = 1;
            string token = "random-secure-token-123";
            DateTime expiresAt = DateTime.Now.AddDays(7);

            // Act
            var result = RefreshToken.Create(appUserId, token, expiresAt);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.AppUserId.Should().Be(appUserId);
            result.Value.Token.Should().Be(token);
            result.Value.ExpiresAt.Should().Be(expiresAt);
            result.Value.IsActive.Should().BeTrue();
            result.Value.RevokedAt.Should().BeNull();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceToken_ShouldReturnFailureResult(string? invalidToken)
        {
            // Act
            var result = RefreshToken.Create(1, invalidToken, DateTime.Now.AddDays(1));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RefreshToken.EmptyToken");
            result.Error.Message.Should().Be("Token is required.");
        }

        [Fact]
        public void Create_WithPastOrPresentExpirationDate_ShouldReturnFailureResult()
        {
            // Arrange
            DateTime pastExpiration = DateTime.Now.AddMinutes(-5);

            // Act
            var result = RefreshToken.Create(1, "valid-token", pastExpiration);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RefreshToken.InvalidExpiration");
            result.Error.Message.Should().Be("Expiration must be in the future.");
        }

        [Fact]
        public void IsValid_WhenTokenIsNotRevokedNotExpiredAndIsActive_ShouldReturnTrue()
        {
            // Arrange
            var token = RefreshToken.Create(1, "valid-token", DateTime.Now.AddDays(1)).Value;

            // Act
            var isValid = token.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WhenTokenIsRevoked_ShouldReturnFalse()
        {
            // Arrange
            var token = RefreshToken.Create(1, "valid-token", DateTime.Now.AddDays(1)).Value;
            token.Revoke();

            // Act
            var isValid = token.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task IsValid_WhenTokenIsExpired_ShouldReturnFalse()
        {
            // Arrange - Cria um token que expira em 50 milissegundos
            var expiresAt = DateTime.Now.AddMilliseconds(50);
            var token = RefreshToken.Create(1, "valid-token", expiresAt).Value;

            // Aguarda o tempo passar para o token expirar
            await Task.Delay(100);

            // Act
            var isValid = token.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenTokenIsNotActive_ShouldReturnFalse()
        {
            // Arrange
            var token = RefreshToken.Create(1, "valid-token", DateTime.Now.AddDays(1)).Value;

            // Simulando a desativação da entidade via reflexão (pois não há método Deactivate exposto)
            var property = typeof(RefreshToken).GetProperty(nameof(RefreshToken.IsActive));
            property!.SetValue(token, false);

            // Act
            var isValid = token.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Revoke_ShouldSetRevokedAtAndUpdatedAt()
        {
            // Arrange
            var token = RefreshToken.Create(1, "valid-token", DateTime.Now.AddDays(1)).Value;

            // Act
            token.Revoke();

            // Assert
            token.RevokedAt.Should().NotBeNull();
            token.RevokedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            token.UpdatedAt.Should().NotBeNull();
            token.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(RefreshToken), true) as RefreshToken;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
