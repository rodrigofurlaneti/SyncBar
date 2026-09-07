using FluentAssertions;
using SyncBar.Application.Features.CustomerAppUser.Remove;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.Remove;

public sealed class RemoveCustomerAppUserCommandValidatorTests
{
    private readonly RemoveCustomerAppUserCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new RemoveCustomerAppUserCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new RemoveCustomerAppUserCommand(id)).IsValid.Should().BeFalse();
}
