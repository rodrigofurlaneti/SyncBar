using FluentAssertions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodFinancialEventTests
    {
        private static Result<IfoodFinancialEvent> CreateValidEvent(string ifoodEventId = "evt-123")
        {
            var competenceDate = new DateTime(2026, 8, 25);
            var periodStart = new DateTime(2026, 8, 18);
            var periodEnd = new DateTime(2026, 8, 24);
            var settlementExpectedDate = new DateTime(2026, 9, 1);

            return IfoodFinancialEvent.Create(
                branchId: 1,
                IfoodEventId: ifoodEventId,
                name: "Order settlement",
                description: "Repasse referente ao pedido",
                trigger: "ORDER_PLACED",
                amount: 45.90m,
                hasTransferImpact: true,
                competenceDate: competenceDate,
                periodStart: periodStart,
                periodEnd: periodEnd,
                settlementExpectedDate: settlementExpectedDate,
                referenceType: "ORDER",
                referenceId: "order-abc",
                rawPayload: "{\"eventId\":\"evt-123\"}");
        }

        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = CreateValidEvent();

            // Assert
            result.IsSuccess.Should().BeTrue();
            var financialEvent = result.Value;
            financialEvent.Should().NotBeNull();
            financialEvent.BranchId.Should().Be(1);
            financialEvent.IfoodEventId.Should().Be("evt-123");
            financialEvent.Name.Should().Be("Order settlement");
            financialEvent.Description.Should().Be("Repasse referente ao pedido");
            financialEvent.Trigger.Should().Be("ORDER_PLACED");
            financialEvent.Amount.Should().Be(45.90m);
            financialEvent.HasTransferImpact.Should().BeTrue();
            financialEvent.CompetenceDate.Should().Be(new DateTime(2026, 8, 25));
            financialEvent.PeriodStart.Should().Be(new DateTime(2026, 8, 18));
            financialEvent.PeriodEnd.Should().Be(new DateTime(2026, 8, 24));
            financialEvent.SettlementExpectedDate.Should().Be(new DateTime(2026, 9, 1));
            financialEvent.ReferenceType.Should().Be("ORDER");
            financialEvent.ReferenceId.Should().Be("order-abc");
            financialEvent.RawPayload.Should().Be("{\"eventId\":\"evt-123\"}");
            financialEvent.IsActive.Should().BeTrue();
            financialEvent.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            financialEvent.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceIfoodEventId_ShouldReturnFailureResult(string? invalidEventId)
        {
            // Act
            var result = CreateValidEvent(invalidEventId!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodFinancialEvent.MissingEventId");
            result.Error.Message.Should().Be("Financial event requires an Ifood event id.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodFinancialEvent), true) as IfoodFinancialEvent;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
