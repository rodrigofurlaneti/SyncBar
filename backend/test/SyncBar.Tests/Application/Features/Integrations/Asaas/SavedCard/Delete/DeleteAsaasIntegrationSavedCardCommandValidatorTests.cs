using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Delete;

public sealed class DeleteAsaasIntegrationSavedCardCommandValidatorTests
{
    private readonly DeleteAsaasIntegrationSavedCardCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new DeleteAsaasIntegrationSavedCardCommand(1, 1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new DeleteAsaasIntegrationSavedCardCommand(id, 1, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new DeleteAsaasIntegrationSavedCardCommand(1, customerId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new DeleteAsaasIntegrationSavedCardCommand(1, 1, companyId)).IsValid.Should().BeFalse();
}
