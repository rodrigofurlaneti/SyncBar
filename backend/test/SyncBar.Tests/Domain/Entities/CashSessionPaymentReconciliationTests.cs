using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CashSessionPaymentReconciliationTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = CashSessionPaymentReconciliation.Create(
                cashSessionId: 1, paymentMethodId: 2, expectedAmount: 200m, countedAmount: 190m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var reconciliation = result.Value;
            reconciliation.CashSessionId.Should().Be(1);
            reconciliation.PaymentMethodId.Should().Be(2);
            reconciliation.ExpectedAmount.Should().Be(200m);
            reconciliation.CountedAmount.Should().Be(190m);
            reconciliation.DifferenceAmount.Should().Be(-10m);
            reconciliation.IsActive.Should().BeTrue();
            reconciliation.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Create_WithCountedAmountAboveExpected_ShouldComputePositiveDifference()
        {
            // Act
            var result = CashSessionPaymentReconciliation.Create(1, 2, expectedAmount: 100m, countedAmount: 120m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.DifferenceAmount.Should().Be(20m);
        }

        [Fact]
        public void Create_WithInvalidCashSessionId_ShouldReturnFailureResult()
        {
            // Act
            var result = CashSessionPaymentReconciliation.Create(cashSessionId: 0, paymentMethodId: 2, expectedAmount: 100m, countedAmount: 100m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSessionPaymentReconciliation.InvalidCashSession");
        }

        [Fact]
        public void Create_WithInvalidPaymentMethodId_ShouldReturnFailureResult()
        {
            // Act
            var result = CashSessionPaymentReconciliation.Create(cashSessionId: 1, paymentMethodId: 0, expectedAmount: 100m, countedAmount: 100m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSessionPaymentReconciliation.InvalidPaymentMethod");
        }

        [Fact]
        public void Create_WithNegativeCountedAmount_ShouldReturnFailureResult()
        {
            // Act
            var result = CashSessionPaymentReconciliation.Create(cashSessionId: 1, paymentMethodId: 2, expectedAmount: 100m, countedAmount: -1m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSessionPaymentReconciliation.InvalidCountedAmount");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var reconciliation = CashSessionPaymentReconciliation.Create(1, 2, 100m, 100m).Value;

            // Act
            reconciliation.Deactivate();

            // Assert
            reconciliation.IsActive.Should().BeFalse();
            reconciliation.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CashSessionPaymentReconciliation), true) as CashSessionPaymentReconciliation;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
