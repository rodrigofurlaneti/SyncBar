using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AsaasIntegrationSavedCardTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var result = AsaasIntegrationSavedCard.Create(1, 2, "token-1", "VISA", "1111", "Fulano", "12", "2030", true);

            result.IsSuccess.Should().BeTrue();
            var card = result.Value;
            card.CustomerId.Should().Be(1);
            card.CompanyId.Should().Be(2);
            card.CreditCardToken.Should().Be("token-1");
            card.CardBrand.Should().Be("VISA");
            card.Last4Digits.Should().Be("1111");
            card.HolderName.Should().Be("Fulano");
            card.ExpiryMonth.Should().Be("12");
            card.ExpiryYear.Should().Be("2030");
            card.IsDefault.Should().BeTrue();
            card.IsActive.Should().BeTrue();
            card.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            card.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithDefaultArguments_ShouldNotBeDefaultAndHaveNullOptionalFields()
        {
            var result = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111");

            result.IsSuccess.Should().BeTrue();
            result.Value.IsDefault.Should().BeFalse();
            result.Value.HolderName.Should().BeNull();
            result.Value.ExpiryMonth.Should().BeNull();
            result.Value.ExpiryYear.Should().BeNull();
        }

        [Fact]
        public void Create_WithInvalidCustomerId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationSavedCard.Create(0, 1, "token-1", "VISA", "1111");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CustomerId.Invalid");
        }

        [Fact]
        public void Create_WithInvalidCompanyId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationSavedCard.Create(1, 0, "token-1", "VISA", "1111");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CompanyId.Invalid");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyCreditCardToken_ShouldReturnFailure(string? token)
        {
            var result = AsaasIntegrationSavedCard.Create(1, 1, token!, "VISA", "1111");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CreditCardToken.Empty");
        }

        [Fact]
        public void UpdateDetails_WithNewHolderName_ShouldUpdateItAndUpdatedAt()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", "Old Name").Value;

            card.UpdateDetails(holderName: "New Name");

            card.HolderName.Should().Be("New Name");
            card.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithBlankHolderName_ShouldKeepExistingValue(string? holderName)
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", "Old Name").Value;

            card.UpdateDetails(holderName: holderName);

            card.HolderName.Should().Be("Old Name");
        }

        [Fact]
        public void UpdateDetails_WithNewExpiryMonthAndYear_ShouldUpdateThem()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", expiryMonth: "01", expiryYear: "2025").Value;

            card.UpdateDetails(expiryMonth: "12", expiryYear: "2030");

            card.ExpiryMonth.Should().Be("12");
            card.ExpiryYear.Should().Be("2030");
        }

        [Fact]
        public void UpdateDetails_WithIsDefaultTrue_ShouldSetIsDefault()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111").Value;

            card.UpdateDetails(isDefault: true);

            card.IsDefault.Should().BeTrue();
        }

        [Fact]
        public void UpdateDetails_WithIsDefaultFalse_ShouldUnsetIsDefault()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", isDefault: true).Value;

            card.UpdateDetails(isDefault: false);

            card.IsDefault.Should().BeFalse();
        }

        [Fact]
        public void UpdateDetails_WithoutIsDefault_ShouldKeepExistingValue()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", isDefault: true).Value;

            card.UpdateDetails();

            card.IsDefault.Should().BeTrue();
        }

        [Fact]
        public void SetAsDefault_ShouldSetIsDefaultTrueAndUpdatedAt()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111").Value;

            card.SetAsDefault();

            card.IsDefault.Should().BeTrue();
            card.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void RemoveAsDefault_ShouldSetIsDefaultFalseAndUpdatedAt()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", isDefault: true).Value;

            card.RemoveAsDefault();

            card.IsDefault.Should().BeFalse();
            card.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111").Value;

            card.Deactivate();

            card.IsActive.Should().BeFalse();
            card.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(AsaasIntegrationSavedCard), true) as AsaasIntegrationSavedCard;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
