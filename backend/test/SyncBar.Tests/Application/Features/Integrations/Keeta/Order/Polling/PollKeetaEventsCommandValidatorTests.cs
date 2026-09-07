using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Polling;

public sealed class PollKeetaEventsCommandValidatorTests
{
    private readonly PollKeetaEventsCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new PollKeetaEventsCommand(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new PollKeetaEventsCommand(companyId, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeBranchId_ShouldBeInvalid()
        => _validator.Validate(new PollKeetaEventsCommand(1, -1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroBranchId_ShouldBeValid()
        => _validator.Validate(new PollKeetaEventsCommand(1, 0)).IsValid.Should().BeTrue();
}
