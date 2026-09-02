using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ComandaTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long comandaStatusId = 1;
            string code = "COM-001";

            // Act
            var result = Comanda.Create(branchId, comandaStatusId, code);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var comanda = result.Value;
            comanda.Should().NotBeNull();
            comanda.BranchId.Should().Be(branchId);
            comanda.ComandaStatusId.Should().Be(comandaStatusId);
            comanda.Code.Should().Be(code);
            comanda.IsActive.Should().BeTrue();
            comanda.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            comanda.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCode_ShouldReturnFailureResult(string? invalidCode)
        {
            // Act
            var result = Comanda.Create(1, 1, invalidCode);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Comanda.EmptyCode");
            result.Error.Message.Should().Be("Code is required.");
        }

        [Fact]
        public void ChangeStatus_ShouldUpdateComandaStatusIdAndSetUpdatedAt()
        {
            // Arrange
            var comanda = Comanda.Create(1, 1, "COM-001").Value;

            // Act
            comanda.ChangeStatus(2);

            // Assert
            comanda.ComandaStatusId.Should().Be(2);
            comanda.UpdatedAt.Should().NotBeNull();
            comanda.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var comanda = Comanda.Create(1, 1, "COM-001").Value;

            // Act
            comanda.Touch();

            // Assert
            comanda.UpdatedAt.Should().NotBeNull();
            comanda.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var comanda = Comanda.Create(1, 1, "COM-001").Value;

            // Act
            comanda.Deactivate();

            // Assert
            comanda.IsActive.Should().BeFalse();
            comanda.UpdatedAt.Should().NotBeNull();
            comanda.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Comanda), true) as Comanda;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
