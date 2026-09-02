using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CashRegisterTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            string name = "Caixa 1";

            // Act
            var result = CashRegister.Create(branchId, name);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var cashRegister = result.Value;
            cashRegister.Should().NotBeNull();
            cashRegister.BranchId.Should().Be(branchId);
            cashRegister.Name.Should().Be(name);
            cashRegister.IsActive.Should().BeTrue();
            cashRegister.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            cashRegister.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = CashRegister.Create(1, invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashRegister.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var cashRegister = CashRegister.Create(1, "Caixa 1").Value;

            // Act
            cashRegister.Touch();

            // Assert
            cashRegister.UpdatedAt.Should().NotBeNull();
            cashRegister.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var cashRegister = CashRegister.Create(1, "Caixa 1").Value;

            // Act
            cashRegister.Deactivate();

            // Assert
            cashRegister.IsActive.Should().BeFalse();
            cashRegister.UpdatedAt.Should().NotBeNull();
            cashRegister.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CashRegister), true) as CashRegister;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
