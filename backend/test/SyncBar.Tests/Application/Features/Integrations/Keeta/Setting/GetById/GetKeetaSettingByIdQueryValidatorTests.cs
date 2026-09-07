using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetById;

public sealed class GetKeetaSettingByIdQueryValidatorTests
{
    private readonly GetKeetaSettingByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaSettingByIdQuery(id)).IsValid.Should().BeFalse();
}
