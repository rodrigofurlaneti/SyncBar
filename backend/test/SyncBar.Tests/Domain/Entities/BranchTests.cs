using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class BranchTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Main Branch";
            string cnpj = "12.345.678/0001-99";
            string phone = "11999999999";
            string street = "Main St";
            string number = "100";
            string district = "Downtown";
            string city = "Sao Paulo";
            string state = "SP";
            string zipCode = "01000-000";

            // Act
            var result = Branch.Create(companyId, name, cnpj, phone, street, number, district, city, state, zipCode);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var branch = result.Value;
            branch.Should().NotBeNull();
            branch.CompanyId.Should().Be(companyId);
            branch.Name.Should().Be(name);
            branch.Cnpj.Should().Be(cnpj);
            branch.Phone.Should().Be(phone);
            branch.AddressStreet.Should().Be(street);
            branch.AddressNumber.Should().Be(number);
            branch.AddressDistrict.Should().Be(district);
            branch.AddressCity.Should().Be(city);
            branch.AddressState.Should().Be(state);
            branch.AddressZipCode.Should().Be(zipCode);
            branch.SelfServiceEmployeeId.Should().BeNull();
            branch.IsActive.Should().BeTrue();
            branch.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            branch.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNullOptionalArguments_ShouldReturnSuccessResult()
        {
            // Act
            var result = Branch.Create(1, "Branch Without Extras", null, null, null, null, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Cnpj.Should().BeNull();
            result.Value.Phone.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Branch.Create(1, invalidName!, null, null, null, null, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Branch.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void SetSelfServiceEmployee_WithEmployeeId_ShouldSetIdAndUpdatedAt()
        {
            // Arrange
            var branch = Branch.Create(1, "Branch", null, null, null, null, null, null, null, null).Value;

            // Act
            branch.SetSelfServiceEmployee(50);

            // Assert
            branch.SelfServiceEmployeeId.Should().Be(50);
            branch.UpdatedAt.Should().NotBeNull();
            branch.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SetSelfServiceEmployee_WithNull_ShouldClearId()
        {
            // Arrange
            var branch = Branch.Create(1, "Branch", null, null, null, null, null, null, null, null).Value;
            branch.SetSelfServiceEmployee(50);

            // Act
            branch.SetSelfServiceEmployee(null);

            // Assert
            branch.SelfServiceEmployeeId.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var branch = Branch.Create(1, "Branch", null, null, null, null, null, null, null, null).Value;

            // Act
            branch.Touch();

            // Assert
            branch.UpdatedAt.Should().NotBeNull();
            branch.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var branch = Branch.Create(1, "Branch", null, null, null, null, null, null, null, null).Value;

            // Act
            branch.Deactivate();

            // Assert
            branch.IsActive.Should().BeFalse();
            branch.UpdatedAt.Should().NotBeNull();
            branch.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Branch), true) as Branch;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
