using FluentAssertions;
using SyncBar.Application.Features.Orders.StartPreparation;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.StartPreparation;

public sealed class StartOrderPreparationCommandValidatorTests
{
    private readonly StartOrderPreparationCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveCustomerOrderId_ShouldBeValid()
        => _validator.Validate(new StartOrderPreparationCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerOrderId_ShouldBeInvalid(long customerOrderId)
        => _validator.Validate(new StartOrderPreparationCommand(customerOrderId)).IsValid.Should().BeFalse();
}
