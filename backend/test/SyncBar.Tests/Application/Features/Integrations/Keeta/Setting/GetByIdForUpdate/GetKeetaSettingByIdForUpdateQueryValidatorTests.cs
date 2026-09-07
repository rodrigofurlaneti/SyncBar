using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate;

public sealed class GetKeetaSettingByIdForUpdateQueryValidatorTests
{
    private readonly GetKeetaSettingByIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaSettingByIdForUpdateQuery(id)).IsValid.Should().BeFalse();
}
