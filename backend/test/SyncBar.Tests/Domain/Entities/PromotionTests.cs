using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PromotionTests
    {
        [Fact]
        public void Create_WithValidArgumentsAndDefaultType_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long productId = 10;
            string name = "Happy Hour";
            int dayOfWeek = 5;
            int startMinuteOfDay = 960; // 16:00
            int endMinuteOfDay = 1200;  // 20:00

            // Act
            var result = Promotion.Create(branchId, productId, name, dayOfWeek, startMinuteOfDay, endMinuteOfDay);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var promotion = result.Value;
            promotion.Should().NotBeNull();
            promotion.BranchId.Should().Be(branchId);
            promotion.ProductId.Should().Be(productId);
            promotion.Name.Should().Be(name);
            promotion.DayOfWeek.Should().Be(dayOfWeek);
            promotion.StartMinuteOfDay.Should().Be(startMinuteOfDay);
            promotion.EndMinuteOfDay.Should().Be(endMinuteOfDay);
            promotion.PromotionTypeId.Should().Be(PromotionTypeIds.EmDobro);
            promotion.DiscountRate.Should().BeNull();
            promotion.IsActive.Should().BeTrue();
            promotion.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            promotion.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithDescontoTypeAndValidRate_ShouldReturnSuccessResultWithDiscountRate()
        {
            // Act
            var result = Promotion.Create(1, 10, "Desconto de Terca", 2, 600, 720, PromotionTypeIds.Desconto, 0.25m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PromotionTypeId.Should().Be(PromotionTypeIds.Desconto);
            result.Value.DiscountRate.Should().Be(0.25m);
        }

        [Fact]
        public void Create_WithEmDobroTypeAndDiscountRateProvided_ShouldIgnoreDiscountRate()
        {
            // Act
            var result = Promotion.Create(1, 10, "Em Dobro", 2, 600, 720, PromotionTypeIds.EmDobro, 0.25m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.DiscountRate.Should().BeNull(); // discount rate only kept for Desconto
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Promotion.Create(1, 10, invalidName!, 2, 600, 720);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void Create_WithInvalidDayOfWeek_ShouldReturnFailureResult(int invalidDay)
        {
            // Act
            var result = Promotion.Create(1, 10, "Happy Hour", invalidDay, 600, 720);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.InvalidDay");
            result.Error.Message.Should().Be("Day of week must be between 0 (Sunday) and 6 (Saturday).");
        }

        [Theory]
        [InlineData(-1, 720)]
        [InlineData(600, 0)]
        [InlineData(600, 1441)]
        public void Create_WithMinutesOutsideDayBounds_ShouldReturnFailureResult(int startMinuteOfDay, int endMinuteOfDay)
        {
            // Act
            var result = Promotion.Create(1, 10, "Happy Hour", 2, startMinuteOfDay, endMinuteOfDay);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.InvalidWindow");
            result.Error.Message.Should().Be("Minutes must be within the day.");
        }

        [Fact]
        public void Create_WithStartAfterOrEqualEnd_ShouldReturnFailureResult()
        {
            // Act
            var result = Promotion.Create(1, 10, "Happy Hour", 2, 720, 720);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.InvalidWindow");
            result.Error.Message.Should().Be("Start must be before end.");
        }

        [Fact]
        public void Create_WithInvalidPromotionType_ShouldReturnFailureResult()
        {
            // Act
            var result = Promotion.Create(1, 10, "Happy Hour", 2, 600, 720, promotionTypeId: 999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.InvalidType");
            result.Error.Message.Should().Be("Promotion type must be EmDobro or Desconto.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-0.1)]
        public void Create_WithDescontoTypeAndInvalidDiscountRate_ShouldReturnFailureResult(double? invalidRate)
        {
            // Act
            var result = Promotion.Create(1, 10, "Desconto", 2, 600, 720, PromotionTypeIds.Desconto, (decimal?)invalidRate);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Promotion.InvalidDiscount");
            result.Error.Message.Should().Be("Discount rate must be between 0 and 1.");
        }

        [Fact]
        public void IsActiveAt_WithMatchingDayAndWithinWindow_ShouldReturnTrue()
        {
            // Arrange: Wednesday (3), window 16:00-20:00
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value;
            var wednesday1800 = new DateTime(2026, 9, 2, 18, 0, 0); // 2026-09-02 is a Wednesday

            // Act
            var isActive = promotion.IsActiveAt(wednesday1800);

            // Assert
            isActive.Should().BeTrue();
        }

        [Fact]
        public void IsActiveAt_WithMatchingDayButOutsideWindow_ShouldReturnFalse()
        {
            // Arrange
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value;
            var wednesday1500 = new DateTime(2026, 9, 2, 15, 0, 0);

            // Act
            var isActive = promotion.IsActiveAt(wednesday1500);

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void IsActiveAt_WithDifferentDayOfWeek_ShouldReturnFalse()
        {
            // Arrange
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value; // Wednesday only
            var thursday1800 = new DateTime(2026, 9, 3, 18, 0, 0);

            // Act
            var isActive = promotion.IsActiveAt(thursday1800);

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void IsActiveAt_WhenPromotionIsDeactivated_ShouldReturnFalse()
        {
            // Arrange
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value;
            promotion.Deactivate();
            var wednesday1800 = new DateTime(2026, 9, 2, 18, 0, 0);

            // Act
            var isActive = promotion.IsActiveAt(wednesday1800);

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void IsActiveAt_AtExactEndMinute_ShouldReturnFalse()
        {
            // Arrange: window ends exclusive at 20:00 (minute 1200)
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value;
            var wednesdayExactEnd = new DateTime(2026, 9, 2, 20, 0, 0);

            // Act
            var isActive = promotion.IsActiveAt(wednesdayExactEnd);

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var promotion = Promotion.Create(1, 10, "Happy Hour", 3, 960, 1200).Value;

            // Act
            promotion.Deactivate();

            // Assert
            promotion.IsActive.Should().BeFalse();
            promotion.UpdatedAt.Should().NotBeNull();
            promotion.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Promotion), true) as Promotion;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
