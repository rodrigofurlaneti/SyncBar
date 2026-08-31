using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class SalePaymentTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long saleId = 100;
            long paymentMethodId = 3;
            decimal amount = 45.90m;
            decimal? changeAmount = 4.10m;
            string? authorizationCode = "AUTH-123";

            // Act
            var result = SalePayment.Create(saleId, paymentMethodId, amount, changeAmount, authorizationCode);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.SaleId.Should().Be(saleId);
            result.Value.PaymentMethodId.Should().Be(paymentMethodId);
            result.Value.Amount.Should().Be(amount);
            result.Value.ChangeAmount.Should().Be(changeAmount);
            result.Value.AuthorizationCode.Should().Be(authorizationCode);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithoutChangeAmountOrAuthorizationCode_ShouldReturnSuccessResultWithNullOptionalFields()
        {
            // Arrange & Act
            var result = SalePayment.Create(100, 1, 30m, null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.ChangeAmount.Should().BeNull();
            result.Value.AuthorizationCode.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10.50)]
        public void Create_WithAmountNotGreaterThanZero_ShouldReturnFailure(decimal amount)
        {
            // Act
            var result = SalePayment.Create(100, 1, amount, null, null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("SalePayment.InvalidAmount");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var salePayment = SalePayment.Create(100, 1, 20m, null, null).Value;

            // Act
            salePayment.Deactivate();

            // Assert
            salePayment.IsActive.Should().BeFalse();
            salePayment.UpdatedAt.Should().NotBeNull();
            salePayment.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(SalePayment), true) as SalePayment;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
