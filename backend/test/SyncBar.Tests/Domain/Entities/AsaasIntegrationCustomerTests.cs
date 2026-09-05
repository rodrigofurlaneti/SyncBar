using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AsaasIntegrationCustomerTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var result = AsaasIntegrationCustomer.Create(1, 2, "cus_123");

            result.IsSuccess.Should().BeTrue();
            var customer = result.Value;
            customer.CustomerId.Should().Be(1);
            customer.CompanyId.Should().Be(2);
            customer.AsaasCustomerId.Should().Be("cus_123");
            customer.IsActive.Should().BeTrue();
            customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            customer.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithInvalidCustomerId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationCustomer.Create(0, 1, "cus_123");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CustomerId.Invalid");
        }

        [Fact]
        public void Create_WithInvalidCompanyId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationCustomer.Create(1, 0, "cus_123");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CompanyId.Invalid");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyAsaasCustomerId_ShouldReturnFailure(string? asaasCustomerId)
        {
            var result = AsaasIntegrationCustomer.Create(1, 1, asaasCustomerId!);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("AsaasCustomerId.Empty");
        }

        [Fact]
        public void UpdateAsaasCustomerId_WithValidValue_ShouldUpdateItAndUpdatedAt()
        {
            var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_old").Value;

            customer.UpdateAsaasCustomerId("cus_new");

            customer.AsaasCustomerId.Should().Be("cus_new");
            customer.UpdatedAt.Should().NotBeNull();
            customer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateAsaasCustomerId_WithBlankValue_ShouldNotChangeExistingValue(string? newValue)
        {
            var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_old").Value;

            customer.UpdateAsaasCustomerId(newValue!);

            customer.AsaasCustomerId.Should().Be("cus_old");
            customer.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(AsaasIntegrationCustomer), true) as AsaasIntegrationCustomer;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
