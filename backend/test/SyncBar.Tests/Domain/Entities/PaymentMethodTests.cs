using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PaymentMethodTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties(bool allowsChange)
        {
            // Arrange
            string name = allowsChange ? "Dinheiro" : "Cartao de Credito";

            // Act
            var result = PaymentMethod.Create(name, allowsChange);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.AllowsChange.Should().Be(allowsChange);
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
            var result = PaymentMethod.Create(invalidName!, true);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PaymentMethod.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var paymentMethod = PaymentMethod.Create("Pix", false).Value;

            // Act
            paymentMethod.Touch();

            // Assert
            paymentMethod.UpdatedAt.Should().NotBeNull();
            paymentMethod.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var paymentMethod = PaymentMethod.Create("Pix", false).Value;

            // Act
            paymentMethod.Deactivate();

            // Assert
            paymentMethod.IsActive.Should().BeFalse();
            paymentMethod.UpdatedAt.Should().NotBeNull();
            paymentMethod.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PaymentMethod), true) as PaymentMethod;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
