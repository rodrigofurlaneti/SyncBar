using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // PrinterSetting.Create has no validation branches in the source (it always returns success)
    // — no Create failure tests exist here on purpose.
    public class PrinterSettingTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties(bool printOrdersEnabled, bool printBillsEnabled)
        {
            // Arrange
            long branchId = 1;

            // Act
            var result = PrinterSetting.Create(branchId, printOrdersEnabled, printBillsEnabled);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var setting = result.Value;
            setting.Should().NotBeNull();
            setting.BranchId.Should().Be(branchId);
            setting.PrintOrdersEnabled.Should().Be(printOrdersEnabled);
            setting.PrintBillsEnabled.Should().Be(printBillsEnabled);
            setting.IsActive.Should().BeTrue();
            setting.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            setting.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Update_WithNewValues_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var setting = PrinterSetting.Create(1, true, true).Value;

            // Act
            setting.Update(false, false);

            // Assert
            setting.PrintOrdersEnabled.Should().BeFalse();
            setting.PrintBillsEnabled.Should().BeFalse();
            setting.UpdatedAt.Should().NotBeNull();
            setting.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PrinterSetting), true) as PrinterSetting;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
