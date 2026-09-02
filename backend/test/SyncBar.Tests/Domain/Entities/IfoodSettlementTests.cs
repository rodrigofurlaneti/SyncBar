using FluentAssertions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodSettlementTests
    {
        private static Result<IfoodSettlement> CreateValidSettlement(string ifoodSettlementId = "stl-123")
            => IfoodSettlement.Create(
                branchId: 1,
                IfoodSettlementId: ifoodSettlementId,
                type: "REPASSE",
                product: "IFOOD",
                amount: 1500.75m,
                status: "PENDING",
                paymentDate: null,
                bankCode: null,
                bankAgency: null,
                bankAccount: null,
                rawPayload: "{\"id\":\"stl-123\"}");

        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = CreateValidSettlement();

            // Assert
            result.IsSuccess.Should().BeTrue();
            var settlement = result.Value;
            settlement.Should().NotBeNull();
            settlement.BranchId.Should().Be(1);
            settlement.IfoodSettlementId.Should().Be("stl-123");
            settlement.Type.Should().Be("REPASSE");
            settlement.Product.Should().Be("IFOOD");
            settlement.Amount.Should().Be(1500.75m);
            settlement.Status.Should().Be("PENDING");
            settlement.PaymentDate.Should().BeNull();
            settlement.BankCode.Should().BeNull();
            settlement.BankAgency.Should().BeNull();
            settlement.BankAccount.Should().BeNull();
            settlement.RawPayload.Should().Be("{\"id\":\"stl-123\"}");
            settlement.IsActive.Should().BeTrue();
            settlement.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            settlement.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceSettlementId_ShouldReturnFailureResult(string? invalidId)
        {
            // Act
            var result = CreateValidSettlement(invalidId!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodSettlement.MissingSettlementId");
            result.Error.Message.Should().Be("Settlement requires an Ifood settlement id.");
        }

        [Fact]
        public void UpdateFromSync_ShouldUpdateStatusAndBankDataAndSetUpdatedAt()
        {
            // Arrange
            var settlement = CreateValidSettlement().Value;
            var paymentDate = new DateTime(2026, 9, 8);

            // Act
            settlement.UpdateFromSync(
                status: "SUCCEED",
                paymentDate: paymentDate,
                bankCode: "341",
                bankAgency: "1234",
                bankAccount: "56789-0",
                rawPayload: "{\"id\":\"stl-123\",\"status\":\"SUCCEED\"}");

            // Assert
            settlement.Status.Should().Be("SUCCEED");
            settlement.PaymentDate.Should().Be(paymentDate);
            settlement.BankCode.Should().Be("341");
            settlement.BankAgency.Should().Be("1234");
            settlement.BankAccount.Should().Be("56789-0");
            settlement.RawPayload.Should().Be("{\"id\":\"stl-123\",\"status\":\"SUCCEED\"}");
            settlement.UpdatedAt.Should().NotBeNull();
            settlement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodSettlement), true) as IfoodSettlement;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
