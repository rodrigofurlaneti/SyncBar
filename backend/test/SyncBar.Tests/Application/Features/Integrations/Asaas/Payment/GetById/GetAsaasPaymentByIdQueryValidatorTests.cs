using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetById;

public sealed class GetAsaasPaymentByIdQueryValidatorTests
{
    private readonly GetAsaasPaymentByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasPaymentByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasPaymentByIdQuery(id)).IsValid.Should().BeFalse();
}
