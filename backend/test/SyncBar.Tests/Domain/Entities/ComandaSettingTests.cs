using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // ComandaSetting não expõe Touch()/Deactivate() no código real — apenas Create() e
    // Update(). Não há testes para métodos inexistentes.
    public class ComandaSettingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            decimal defaultLimitAmount = 200.00m;

            // Act
            var result = ComandaSetting.Create(branchId, defaultLimitAmount);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var comandaSetting = result.Value;
            comandaSetting.Should().NotBeNull();
            comandaSetting.BranchId.Should().Be(branchId);
            comandaSetting.DefaultLimitAmount.Should().Be(defaultLimitAmount);
            comandaSetting.IsActive.Should().BeTrue();
            comandaSetting.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            comandaSetting.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100.0)]
        public void Create_WithNonPositiveLimit_ShouldReturnFailureResult(decimal invalidLimit)
        {
            // Act
            var result = ComandaSetting.Create(1, invalidLimit);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaSetting.InvalidLimit");
            result.Error.Message.Should().Be("Limit must be greater than zero.");
        }

        [Fact]
        public void Update_WithValidArguments_ShouldUpdateLimitAndSetUpdatedAt()
        {
            // Arrange
            var comandaSetting = ComandaSetting.Create(1, 200.00m).Value;

            // Act
            var result = comandaSetting.Update(350.00m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            comandaSetting.DefaultLimitAmount.Should().Be(350.00m);
            comandaSetting.UpdatedAt.Should().NotBeNull();
            comandaSetting.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50.0)]
        public void Update_WithNonPositiveLimit_ShouldReturnFailureResult(decimal invalidLimit)
        {
            // Arrange
            var comandaSetting = ComandaSetting.Create(1, 200.00m).Value;

            // Act
            var result = comandaSetting.Update(invalidLimit);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaSetting.InvalidLimit");
            result.Error.Message.Should().Be("Limit must be greater than zero.");
            comandaSetting.DefaultLimitAmount.Should().Be(200.00m);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ComandaSetting), true) as ComandaSetting;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
