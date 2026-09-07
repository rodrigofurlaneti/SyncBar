using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;

public sealed class GetByCustomerOrderIdQueryValidatorTests
{
    private readonly GetByCustomerOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCustomerOrderId_ShouldBeValid()
        => _validator.Validate(new GetByCustomerOrderIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerOrderId_ShouldBeInvalid(long customerOrderId)
        => _validator.Validate(new GetByCustomerOrderIdQuery(customerOrderId)).IsValid.Should().BeFalse();
}
