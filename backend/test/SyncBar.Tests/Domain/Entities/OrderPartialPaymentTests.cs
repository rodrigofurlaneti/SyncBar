using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class OrderPartialPaymentTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long customerOrderId = 1;
            long cashSessionId = 2;
            long paymentMethodId = 3;
            long employeeId = 4;
            decimal amount = 50.0m;
            string authorizationCode = "AUTH-999";
            string payerName = "João";

            // Act
            var result = OrderPartialPayment.Create(customerOrderId, cashSessionId, paymentMethodId, employeeId, amount, authorizationCode, payerName);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var payment = result.Value;
            payment.Should().NotBeNull();
            payment.CustomerOrderId.Should().Be(customerOrderId);
            payment.CashSessionId.Should().Be(cashSessionId);
            payment.PaymentMethodId.Should().Be(paymentMethodId);
            payment.EmployeeId.Should().Be(employeeId);
            payment.Amount.Should().Be(amount);
            payment.AuthorizationCode.Should().Be(authorizationCode);
            payment.PayerName.Should().Be(payerName);
            payment.IsActive.Should().BeTrue();
            payment.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            payment.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10.0)]
        public void Create_WithInvalidAmount_ShouldReturnFailureResult(decimal invalidAmount)
        {
            // Act
            var result = OrderPartialPayment.Create(1, 2, 3, 4, invalidAmount, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PartialPayment.InvalidAmount");
            result.Error.Message.Should().Be("Amount must be greater than zero.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var payment = OrderPartialPayment.Create(1, 2, 3, 4, 50.0m, null, null).Value;

            // Act
            payment.Deactivate();

            // Assert
            payment.IsActive.Should().BeFalse();
            payment.UpdatedAt.Should().NotBeNull();
            payment.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OrderPartialPayment), true) as OrderPartialPayment;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
