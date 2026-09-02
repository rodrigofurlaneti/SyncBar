using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // Complement.Create/UpdateExtraPrice/Deactivate são 'internal' (só chamados de dentro do
    // aggregate ComplementGroup) — acessíveis aqui via InternalsVisibleTo de SyncBar.Domain
    // para SyncBar.Tests, mesmo padrão já usado para handlers 'internal sealed' da Application.
    public class ComplementTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long complementGroupId = 1;
            long complementItemId = 10;
            decimal extraPrice = 5.50m;

            // Act
            var result = Complement.Create(complementGroupId, complementItemId, extraPrice);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var complement = result.Value;
            complement.Should().NotBeNull();
            complement.ComplementGroupId.Should().Be(complementGroupId);
            complement.ComplementItemId.Should().Be(complementItemId);
            complement.ExtraPrice.Should().Be(extraPrice);
            complement.IsActive.Should().BeTrue();
            complement.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            complement.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Act
            var result = Complement.Create(1, 10, -1.0m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Complement.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
        }

        [Fact]
        public void UpdateExtraPrice_WithValidArgument_ShouldUpdatePriceAndSetUpdatedAt()
        {
            // Arrange
            var complement = Complement.Create(1, 10, 5.0m).Value;

            // Act
            var result = complement.UpdateExtraPrice(7.5m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            complement.ExtraPrice.Should().Be(7.5m);
            complement.UpdatedAt.Should().NotBeNull();
            complement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void UpdateExtraPrice_WithNegativeValue_ShouldReturnFailureResult()
        {
            // Arrange
            var complement = Complement.Create(1, 10, 5.0m).Value;

            // Act
            var result = complement.UpdateExtraPrice(-3.0m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Complement.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
            complement.ExtraPrice.Should().Be(5.0m);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var complement = Complement.Create(1, 10, 5.0m).Value;

            // Act
            complement.Deactivate();

            // Assert
            complement.IsActive.Should().BeFalse();
            complement.UpdatedAt.Should().NotBeNull();
            complement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Complement), true) as Complement;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
