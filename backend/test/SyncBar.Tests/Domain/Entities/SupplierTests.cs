using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class SupplierTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string legalName = "Supplier Corp Ltd";
            string tradeName = "SupplierCorp";
            string cnpj = "00.000.000/0001-00";
            string email = "contact@suppliercorp.com";
            string phone = "11988887777";

            // Act
            var result = Supplier.Create(companyId, legalName, tradeName, cnpj, email, phone);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CompanyId.Should().Be(companyId);
            result.Value.LegalName.Should().Be(legalName);
            result.Value.TradeName.Should().Be(tradeName);
            result.Value.Cnpj.Should().Be(cnpj);
            result.Value.Email.Should().Be(email);
            result.Value.Phone.Should().Be(phone);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceLegalName_ShouldReturnFailureResult(string invalidLegalName)
        {
            // Act
            var result = Supplier.Create(1, invalidLegalName, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Supplier.EmptyLegalName");
            result.Error.Message.Should().Be("LegalName is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var supplier = Supplier.Create(1, "Supplier Corp Ltd", null, null, null, null).Value;

            // Act
            supplier.Touch();

            // Assert
            supplier.UpdatedAt.Should().NotBeNull();
            supplier.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var supplier = Supplier.Create(1, "Supplier Corp Ltd", null, null, null, null).Value;

            // Act
            supplier.Deactivate();

            // Assert
            supplier.IsActive.Should().BeFalse();
            supplier.UpdatedAt.Should().NotBeNull();
            supplier.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Supplier), true) as Supplier;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
