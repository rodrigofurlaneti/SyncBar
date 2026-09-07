using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate;

public sealed class GetAsaasPaymentByIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasPaymentByIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasPaymentByIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasPaymentByIdForUpdateQuery(id)).IsValid.Should().BeFalse();
}
