using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetById;

public sealed class GetAsaasSettingByIdQueryValidatorTests
{
    private readonly GetAsaasSettingByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasSettingByIdQuery(id)).IsValid.Should().BeFalse();
}
