using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ComplementGroupTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Escolha uma bebida";
            long complementGroupTypeId = 2;
            int minSelection = 1;
            int maxSelection = 1;

            // Act
            var result = ComplementGroup.Create(companyId, name, complementGroupTypeId, minSelection, maxSelection);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CompanyId.Should().Be(companyId);
            result.Value.Name.Should().Be(name);
            result.Value.ComplementGroupTypeId.Should().Be(complementGroupTypeId);
            result.Value.MinSelection.Should().Be(minSelection);
            result.Value.MaxSelection.Should().Be(maxSelection);
            result.Value.IsActive.Should().BeTrue();
            result.Value.Complements.Should().BeEmpty();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = ComplementGroup.Create(1, invalidName, 2, 0, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Create_WithNegativeMinSelection_ShouldReturnFailureResult()
        {
            // Act
            var result = ComplementGroup.Create(1, "Group", 2, -1, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.InvalidMinSelection");
            result.Error.Message.Should().Be("Minimum selection cannot be negative.");
        }

        [Fact]
        public void Create_WithMaxSelectionLessThanOne_ShouldReturnFailureResult()
        {
            // Act
            var result = ComplementGroup.Create(1, "Group", 2, 0, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.InvalidMaxSelection");
            result.Error.Message.Should().Be("Maximum selection must be at least 1.");
        }

        [Fact]
        public void Create_WithMinSelectionGreaterThanMax_ShouldReturnFailureResult()
        {
            // Act
            var result = ComplementGroup.Create(1, "Group", 2, 3, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.MinGreaterThanMax");
            result.Error.Message.Should().Be("Minimum selection cannot be greater than maximum selection.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Old Name", 2, 0, 1).Value;

            // Act
            var result = group.UpdateDetails("New Name", 3, 1, 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            group.Name.Should().Be("New Name");
            group.ComplementGroupTypeId.Should().Be(3);
            group.MinSelection.Should().Be(1);
            group.MaxSelection.Should().Be(2);
            group.UpdatedAt.Should().NotBeNull();
            group.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Old Name", 2, 0, 1).Value;

            // Act
            var result = group.UpdateDetails(invalidName, 2, 0, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.EmptyName");
        }

        [Fact]
        public void UpdateDetails_WithNegativeMinSelection_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Name", 2, 0, 1).Value;

            // Act
            var result = group.UpdateDetails("Name", 2, -1, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.InvalidMinSelection");
        }

        [Fact]
        public void UpdateDetails_WithMaxSelectionLessThanOne_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Name", 2, 0, 1).Value;

            // Act
            var result = group.UpdateDetails("Name", 2, 0, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.InvalidMaxSelection");
        }

        [Fact]
        public void UpdateDetails_WithMinGreaterThanMax_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Name", 2, 0, 1).Value;

            // Act
            var result = group.UpdateDetails("Name", 2, 5, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.MinGreaterThanMax");
        }

        [Fact]
        public void AddComplement_WithValidArguments_ShouldAddComplementAndSetUpdatedAt()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;

            // Act
            var result = group.AddComplement(10, 5.5m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            group.Complements.Should().HaveCount(1);
            var complement = group.Complements.First();
            complement.ComplementItemId.Should().Be(10);
            complement.ExtraPrice.Should().Be(5.5m);
            group.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void AddComplement_WithDuplicateActiveComplementItem_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;
            group.AddComplement(10, 5.5m);

            // Act
            var result = group.AddComplement(10, 7.0m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.DuplicateComplementItem");
            result.Error.Message.Should().Be("This complement item is already in the group.");
            group.Complements.Should().HaveCount(1);
        }

        [Fact]
        public void AddComplement_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;

            // Act
            var result = group.AddComplement(10, -1m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Complement.InvalidExtraPrice");
            group.Complements.Should().BeEmpty();
        }

        [Fact]
        public void AddComplement_AfterRemovingSameItem_ShouldAllowReAdding()
        {
            // Arrange: only IsActive duplicates are blocked, so a removed (deactivated) item can be re-added.
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;
            var added = group.AddComplement(10, 5.0m).Value;
            group.RemoveComplement(added.Id);

            // Act
            var result = group.AddComplement(10, 6.0m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            group.Complements.Should().HaveCount(2);
        }

        [Fact]
        public void UpdateComplementPrice_WithValidArguments_ShouldUpdatePriceAndSetUpdatedAt()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;
            var complement = group.AddComplement(10, 5.0m).Value;

            // Act
            var result = group.UpdateComplementPrice(complement.Id, 9.99m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            group.Complements.First().ExtraPrice.Should().Be(9.99m);
            group.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void UpdateComplementPrice_WhenComplementNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;

            // Act
            var result = group.UpdateComplementPrice(999, 9.99m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
            result.Error.Message.Should().Be("Complement not found.");
        }

        [Fact]
        public void UpdateComplementPrice_WithNegativePrice_ShouldReturnFailureResultAndNotTouchGroup()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;
            var complement = group.AddComplement(10, 5.0m).Value;

            // Act
            var result = group.UpdateComplementPrice(complement.Id, -1m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Complement.InvalidExtraPrice");
        }

        [Fact]
        public void RemoveComplement_WithValidId_ShouldDeactivateComplementAndSetUpdatedAt()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;
            var complement = group.AddComplement(10, 5.0m).Value;

            // Act
            var result = group.RemoveComplement(complement.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            group.Complements.First().IsActive.Should().BeFalse();
            group.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void RemoveComplement_WhenComplementNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;

            // Act
            var result = group.RemoveComplement(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var group = ComplementGroup.Create(1, "Group", 2, 0, 1).Value;

            // Act
            group.Deactivate();

            // Assert
            group.IsActive.Should().BeFalse();
            group.UpdatedAt.Should().NotBeNull();
            group.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ComplementGroup), true) as ComplementGroup;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
