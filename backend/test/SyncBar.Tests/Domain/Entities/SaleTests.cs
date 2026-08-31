using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class SaleTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectCalculations()
        {
            // Arrange
            long branchId = 1;
            long customerOrderId = 10;
            long cashSessionId = 5;
            long employeeId = 2;
            long saleNumber = 10001;
            decimal subtotal = 100.00m;
            decimal discount = 10.00m;
            decimal serviceFee = 5.00m;

            // Act
            var result = Sale.Create(branchId, customerOrderId, cashSessionId, employeeId, saleNumber, subtotal, discount, serviceFee);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var sale = result.Value;
            sale.Should().NotBeNull();
            sale.BranchId.Should().Be(branchId);
            sale.CustomerOrderId.Should().Be(customerOrderId);
            sale.CashSessionId.Should().Be(cashSessionId);
            sale.EmployeeId.Should().Be(employeeId);
            sale.SaleNumber.Should().Be(saleNumber);
            sale.SubtotalAmount.Should().Be(subtotal);
            sale.DiscountAmount.Should().Be(discount);
            sale.ServiceFeeAmount.Should().Be(serviceFee);

            // Total = 100 - 10 + 5 = 95
            sale.TotalAmount.Should().Be(95.00m);

            sale.IsActive.Should().BeTrue();
            sale.Payments.Should().BeEmpty();
            sale.SoldAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            sale.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            sale.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(-10.0, 0.0, 0.0)]
        [InlineData(0.0, -5.0, 0.0)]
        [InlineData(0.0, 0.0, -2.0)]
        public void Create_WithNegativeAmounts_ShouldReturnFailureResult(decimal subtotal, decimal discount, decimal serviceFee)
        {
            // Act
            var result = Sale.Create(1, 10, 5, 2, 10001, subtotal, discount, serviceFee);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Sale.InvalidAmounts");
            result.Error.Message.Should().Be("Amounts cannot be negative.");
        }

        [Fact]
        public void AddPayment_WithValidArguments_ShouldAddPaymentAndSetUpdatedAt()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value;
            long paymentMethodId = 1;

            // Act
            var result = sale.AddPayment(paymentMethodId, 100.0m, 0m, "AUTH-123", allowsChange: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            sale.Payments.Should().HaveCount(1);
            var payment = sale.Payments.First();
            payment.Amount.Should().Be(100.0m);
            payment.PaymentMethodId.Should().Be(paymentMethodId);

            sale.UpdatedAt.Should().NotBeNull();
            sale.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50.0)]
        public void AddPayment_WithInvalidAmount_ShouldReturnFailureResult(decimal invalidAmount)
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value;

            // Act
            var result = sale.AddPayment(1, invalidAmount, 0m, null, allowsChange: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Sale.InvalidPaymentAmount");
            result.Error.Message.Should().Be("Payment amount must be greater than zero.");
            sale.Payments.Should().BeEmpty();
        }

        [Fact]
        public void AddPayment_WhenChangeIsRequestedButNotAllowed_ShouldReturnFailureResult()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value;

            // Act
            var result = sale.AddPayment(2, 150.0m, 50.0m, null, allowsChange: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Sale.ChangeNotAllowed");
            result.Error.Message.Should().Be("Change is only allowed for cash payments.");
            sale.Payments.Should().BeEmpty();
        }

        [Fact]
        public void EnsureFullyPaid_WhenPaymentsMeetTotal_ShouldReturnSuccessResult()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value; // Total = 100
            sale.AddPayment(1, 150.0m, 50.0m, null, allowsChange: true); // Paid = 150 - 50 = 100

            // Act
            var result = sale.EnsureFullyPaid();

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void EnsureFullyPaid_WithPreviouslyPaid_ShouldReturnSuccessResult()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value; // Total = 100
            sale.AddPayment(1, 40.0m, 0m, null, allowsChange: false); // Paid = 40

            // Act (Simulating 60 already paid previously)
            var result = sale.EnsureFullyPaid(previouslyPaid: 60.0m);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void EnsureFullyPaid_WhenPaymentsDoNotMeetTotal_ShouldReturnFailureResult()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value; // Total = 100
            sale.AddPayment(1, 99.0m, 0m, null, allowsChange: false);

            // Act
            var result = sale.EnsureFullyPaid();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Sale.InsufficientPayment");
            result.Error.Message.Should().Contain("do not cover the sale total");
        }

        [Fact]
        public void EnsureFullyPaid_ShouldIgnoreInactivePayments()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value; // Total = 100
            sale.AddPayment(1, 100.0m, 0m, null, allowsChange: false);

            // Deactivating the payment manually to simulate an aborted or reverted payment
            sale.Payments.First().Deactivate();

            // Act
            var result = sale.EnsureFullyPaid();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Sale.InsufficientPayment");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var sale = Sale.Create(1, 10, 5, 2, 10001, 100.0m, 0m, 0m).Value;

            // Act
            sale.Deactivate();

            // Assert
            sale.IsActive.Should().BeFalse();
            sale.UpdatedAt.Should().NotBeNull();
            sale.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Sale), true) as Sale;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
