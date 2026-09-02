using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CompanyTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string legalName = "Bar do Ze Ltda";
            string tradeName = "Bar do Ze";
            string cnpj = "12345678000199";
            string email = "contato@bardoze.com";
            string phone = "11999998888";

            // Act
            var result = Company.Create(legalName, tradeName, cnpj, email, phone);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LegalName.Should().Be(legalName);
            result.Value.TradeName.Should().Be(tradeName);
            result.Value.Cnpj.Should().Be(cnpj);
            result.Value.Email.Should().Be(email);
            result.Value.Phone.Should().Be(phone);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNullEmailAndPhone_ShouldReturnSuccessResult()
        {
            // Act
            var result = Company.Create("Legal Name", "Trade Name", "12345678000199", null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Email.Should().BeNull();
            result.Value.Phone.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceLegalName_ShouldReturnFailureResult(string? invalidLegalName)
        {
            // Act
            var result = Company.Create(invalidLegalName, "Trade Name", "12345678000199", null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Company.EmptyLegalName");
            result.Error.Message.Should().Be("LegalName is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceTradeName_ShouldReturnFailureResult(string? invalidTradeName)
        {
            // Act
            var result = Company.Create("Legal Name", invalidTradeName, "12345678000199", null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Company.EmptyTradeName");
            result.Error.Message.Should().Be("TradeName is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCnpj_ShouldReturnFailureResult(string? invalidCnpj)
        {
            // Act
            var result = Company.Create("Legal Name", "Trade Name", invalidCnpj, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Company.EmptyCnpj");
            result.Error.Message.Should().Be("Cnpj is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var company = Company.Create("Legal Name", "Trade Name", "12345678000199", null, null).Value;

            // Act
            company.Touch();

            // Assert
            company.UpdatedAt.Should().NotBeNull();
            company.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var company = Company.Create("Legal Name", "Trade Name", "12345678000199", null, null).Value;

            // Act
            company.Deactivate();

            // Assert
            company.IsActive.Should().BeFalse();
            company.UpdatedAt.Should().NotBeNull();
            company.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Company), true) as Company;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
