using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetById;

public sealed class GetAsaasCustomerByIdQueryValidatorTests
{
    private readonly GetAsaasCustomerByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasCustomerByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasCustomerByIdQuery(id)).IsValid.Should().BeFalse();
}
