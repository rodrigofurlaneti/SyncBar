using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Delete;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Delete;

public sealed class DeleteAsaasPaymentCommandValidatorTests
{
    private readonly DeleteAsaasPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_PositivePaymentId_ShouldBeValid()
        => _validator.Validate(new DeleteAsaasPaymentCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePaymentId_ShouldBeInvalid(long paymentId)
        => _validator.Validate(new DeleteAsaasPaymentCommand(paymentId)).IsValid.Should().BeFalse();
}
