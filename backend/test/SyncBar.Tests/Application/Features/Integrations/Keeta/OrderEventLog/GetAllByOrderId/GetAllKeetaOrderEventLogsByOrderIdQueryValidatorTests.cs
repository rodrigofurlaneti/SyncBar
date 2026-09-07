using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;

public sealed class GetAllKeetaOrderEventLogsByOrderIdQueryValidatorTests
{
    private readonly GetAllKeetaOrderEventLogsByOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_ShouldBeValid()
        => _validator.Validate(new GetAllKeetaOrderEventLogsByOrderIdQuery("order-1")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyOrderId_ShouldBeInvalid()
        => _validator.Validate(new GetAllKeetaOrderEventLogsByOrderIdQuery(string.Empty)).IsValid.Should().BeFalse();
}
