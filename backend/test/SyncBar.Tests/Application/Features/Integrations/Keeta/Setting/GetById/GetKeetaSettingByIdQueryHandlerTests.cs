using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetById;

public sealed class GetKeetaSettingByIdQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByIdQueryHandler _handler;

    public GetKeetaSettingByIdQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByIdQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        setting.SaveCredentials("client-id", "client-secret", "app-id");
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.ClientId.Should().Be("client-id");
        result.Value.HasAccessToken.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SettingWithAccessToken_ShouldReturnHasAccessTokenTrue()
    {
        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        setting.UpdateToken("token-abc", DateTime.UtcNow.AddHours(1));
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAccessToken.Should().BeTrue();
        result.Value.TokenExpiresAtUtc.Should().NotBeNull();
    }
}
