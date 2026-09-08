using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class BranchPaymentMethodSettingTests
    {
        [Fact]
        public void Create_WithValidArgumentsForBranch_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = BranchPaymentMethodSetting.Create(
                companyId: 1,
                branchId: 2,
                enablePix: true,
                enableBoleto: false,
                enableCreditCard: false,
                enableDebitCard: false,
                enableCashMachine: true,
                isActive: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var setting = result.Value;
            setting.CompanyId.Should().Be(1);
            setting.BranchId.Should().Be(2);
            setting.EnablePix.Should().BeTrue();
            setting.EnableBoleto.Should().BeFalse();
            setting.EnableCreditCard.Should().BeFalse();
            setting.EnableDebitCard.Should().BeFalse();
            setting.EnableCashMachine.Should().BeTrue();
            setting.IsActive.Should().BeTrue();
            setting.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            setting.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithoutBranchId_ShouldReturnSuccessResultWithNullBranchId()
        {
            // Act
            var result = BranchPaymentMethodSetting.Create(companyId: 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.BranchId.Should().BeNull();
        }

        [Fact]
        public void Create_WithDefaultArguments_ShouldEnableAllPaymentMethodsAndBeActive()
        {
            // Act
            var result = BranchPaymentMethodSetting.Create(companyId: 1, branchId: 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var setting = result.Value;
            setting.EnablePix.Should().BeTrue();
            setting.EnableBoleto.Should().BeTrue();
            setting.EnableCreditCard.Should().BeTrue();
            setting.EnableDebitCard.Should().BeTrue();
            setting.EnableCashMachine.Should().BeTrue();
            setting.IsActive.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithNonPositiveCompanyId_ShouldReturnFailureResult(long companyId)
        {
            // Act
            var result = BranchPaymentMethodSetting.Create(companyId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CompanyId.Invalid");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithNonPositiveBranchId_ShouldReturnFailureResult(long branchId)
        {
            // Act
            var result = BranchPaymentMethodSetting.Create(companyId: 1, branchId: branchId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("BranchId.Invalid");
        }

        [Fact]
        public void UpdateDetails_WithAllValuesProvided_ShouldOverwriteAllFlagsAndSetUpdatedAt()
        {
            // Arrange
            var setting = BranchPaymentMethodSetting.Create(companyId: 1, branchId: 2).Value;

            // Act
            var result = setting.UpdateDetails(
                enablePix: false,
                enableBoleto: false,
                enableCreditCard: false,
                enableDebitCard: false,
                enableCashMachine: false,
                isActive: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.EnablePix.Should().BeFalse();
            setting.EnableBoleto.Should().BeFalse();
            setting.EnableCreditCard.Should().BeFalse();
            setting.EnableDebitCard.Should().BeFalse();
            setting.EnableCashMachine.Should().BeFalse();
            setting.IsActive.Should().BeFalse();
            setting.UpdatedAt.Should().NotBeNull();
            setting.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void UpdateDetails_WithOnlyOneValueProvided_ShouldLeaveOtherFlagsUnchanged()
        {
            // Arrange
            var setting = BranchPaymentMethodSetting.Create(
                companyId: 1, branchId: 2, enablePix: true, enableBoleto: true,
                enableCreditCard: true, enableDebitCard: true, enableCashMachine: true).Value;

            // Act
            var result = setting.UpdateDetails(enablePix: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.EnablePix.Should().BeFalse();
            setting.EnableBoleto.Should().BeTrue();
            setting.EnableCreditCard.Should().BeTrue();
            setting.EnableDebitCard.Should().BeTrue();
            setting.EnableCashMachine.Should().BeTrue();
        }

        [Fact]
        public void UpdateDetails_WithNoValuesProvided_ShouldOnlySetUpdatedAt()
        {
            // Arrange
            var setting = BranchPaymentMethodSetting.Create(companyId: 1, branchId: 2).Value;

            // Act
            var result = setting.UpdateDetails();

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            // Arrange
            var setting = BranchPaymentMethodSetting.Create(companyId: 1, branchId: 2).Value;

            // Act
            setting.Deactivate();

            // Assert
            setting.IsActive.Should().BeFalse();
            setting.UpdatedAt.Should().NotBeNull();
            setting.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Activate_ShouldSetIsActiveTrueAndUpdatedAt()
        {
            // Arrange
            var setting = BranchPaymentMethodSetting.Create(companyId: 1, branchId: 2, isActive: false).Value;

            // Act
            setting.Activate();

            // Assert
            setting.IsActive.Should().BeTrue();
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(BranchPaymentMethodSetting), true) as BranchPaymentMethodSetting;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
