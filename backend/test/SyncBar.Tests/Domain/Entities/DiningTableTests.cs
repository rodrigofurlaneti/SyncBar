using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class DiningTableTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithReadingValidationFlagsDisabledByDefault()
        {
            // Arrange
            long branchId = 1;
            long tableStatusId = 1;
            int number = 5;
            int? capacity = 4;

            // Act
            var result = DiningTable.Create(branchId, tableStatusId, number, capacity);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.IsQrViewEnabled.Should().BeTrue();
            result.Value.IsCameraInputEnabled.Should().BeFalse();
            result.Value.IsBarcodeEnabled.Should().BeFalse();
            result.Value.IsQrCodeEnabled.Should().BeFalse();
            result.Value.IsActive.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        public void SetReadingValidationSettings_ShouldUpdateAllThreeFlagsAndSetUpdatedAt(
            bool cameraEnabled, bool barcodeEnabled, bool qrCodeEnabled)
        {
            // Arrange
            var table = DiningTable.Create(1, 1, 5, 4).Value;

            // Act
            table.SetReadingValidationSettings(cameraEnabled, barcodeEnabled, qrCodeEnabled);

            // Assert
            table.IsCameraInputEnabled.Should().Be(cameraEnabled);
            table.IsBarcodeEnabled.Should().Be(barcodeEnabled);
            table.IsQrCodeEnabled.Should().Be(qrCodeEnabled);
            table.UpdatedAt.Should().NotBeNull();
            table.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SetReadingValidationSettings_CalledTwice_ShouldReflectLastCallOnly()
        {
            // Arrange
            var table = DiningTable.Create(1, 1, 5, 4).Value;
            table.SetReadingValidationSettings(true, true, true);

            // Act
            table.SetReadingValidationSettings(false, false, false);

            // Assert
            table.IsCameraInputEnabled.Should().BeFalse();
            table.IsBarcodeEnabled.Should().BeFalse();
            table.IsQrCodeEnabled.Should().BeFalse();
        }
    }
}
