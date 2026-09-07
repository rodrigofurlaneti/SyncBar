using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId;

public sealed class ExistsKeetaOrderByKeetaOrderIdQueryValidatorTests
{
    private readonly ExistsKeetaOrderByKeetaOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyKeetaOrderId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaOrderByKeetaOrderIdQuery("keeta-order-1")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyKeetaOrderId_ShouldBeInvalid()
        => _validator.Validate(new ExistsKeetaOrderByKeetaOrderIdQuery(string.Empty)).IsValid.Should().BeFalse();
}
