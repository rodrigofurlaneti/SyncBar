using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CustomerTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Jane Doe";
            string phone = "11999998888";
            string cpf = "12345678900";
            string email = "jane@doe.com";

            // Act
            var result = Customer.Create(companyId, name, phone, cpf, email);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var customer = result.Value;
            customer.Should().NotBeNull();
            customer.CompanyId.Should().Be(companyId);
            customer.Name.Should().Be(name);
            customer.Phone.Should().Be(phone);
            customer.Cpf.Should().Be(cpf);
            customer.Email.Should().Be(email);
            customer.LoyaltyPoints.Should().Be(0);
            customer.IsActive.Should().BeTrue();
            customer.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            customer.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Customer.Create(1, invalidName!, null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Customer.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", "11999998888", "12345678900", "jane@doe.com").Value;

            // Act
            var result = customer.UpdateDetails("Jane Smith", "11888887777", "smith@doe.com");

            // Assert
            result.IsSuccess.Should().BeTrue();
            customer.Name.Should().Be("Jane Smith");
            customer.Phone.Should().Be("11888887777");
            customer.Email.Should().Be("smith@doe.com");
            customer.Cpf.Should().Be("12345678900"); // Cpf is not touched by UpdateDetails
            customer.UpdatedAt.Should().NotBeNull();
            customer.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;

            // Act
            var result = customer.UpdateDetails(invalidName!, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Customer.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
            customer.Name.Should().Be("Jane Doe"); // unchanged
        }

        [Fact]
        public void AddLoyaltyPoints_WithValidPoints_ShouldIncreasePointsAndSetUpdatedAt()
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;

            // Act
            var result = customer.AddLoyaltyPoints(50);

            // Assert
            result.IsSuccess.Should().BeTrue();
            customer.LoyaltyPoints.Should().Be(50);
            customer.UpdatedAt.Should().NotBeNull();
            customer.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void AddLoyaltyPoints_WithNonPositivePoints_ShouldReturnFailureResult(int invalidPoints)
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;

            // Act
            var result = customer.AddLoyaltyPoints(invalidPoints);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Customer.InvalidPoints");
            result.Error.Message.Should().Be("Points must be greater than zero.");
            customer.LoyaltyPoints.Should().Be(0);
        }

        [Fact]
        public void RedeemPoints_WithSufficientPoints_ShouldDecreasePointsAndSetUpdatedAt()
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;
            customer.AddLoyaltyPoints(100);

            // Act
            var result = customer.RedeemPoints(30);

            // Assert
            result.IsSuccess.Should().BeTrue();
            customer.LoyaltyPoints.Should().Be(70);
            customer.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void RedeemPoints_WithNonPositivePoints_ShouldReturnFailureResult(int invalidPoints)
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;
            customer.AddLoyaltyPoints(100);

            // Act
            var result = customer.RedeemPoints(invalidPoints);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Customer.InvalidPoints");
            result.Error.Message.Should().Be("Points must be greater than zero.");
            customer.LoyaltyPoints.Should().Be(100);
        }

        [Fact]
        public void RedeemPoints_WithMorePointsThanAvailable_ShouldReturnFailureResult()
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;
            customer.AddLoyaltyPoints(20);

            // Act
            var result = customer.RedeemPoints(30);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Customer.InsufficientPoints");
            result.Error.Message.Should().Be("Not enough loyalty points.");
            customer.LoyaltyPoints.Should().Be(20);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var customer = Customer.Create(1, "Jane Doe", null, null, null).Value;

            // Act
            customer.Deactivate();

            // Assert
            customer.IsActive.Should().BeFalse();
            customer.UpdatedAt.Should().NotBeNull();
            customer.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Customer), true) as Customer;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
