using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;

public sealed class RefreshKeetaAccessTokenCommandHandlerTests
{
    private readonly IKeetaAccessTokenProvider _tokenProvider = Substitute.For<IKeetaAccessTokenProvider>();
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RefreshKeetaAccessTokenCommandHandler _handler;

    public RefreshKeetaAccessTokenCommandHandlerTests()
    {
        _handler = new RefreshKeetaAccessTokenCommandHandler(_tokenProvider, _settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TokenResolutionFails_ShouldReturnFailure()
    {
        var command = new RefreshKeetaAccessTokenCommand(1, 2);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, true, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Validation("Keeta.SettingNotConfigured", "sem config")));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.SettingNotConfigured");
    }

    [Fact]
    public async Task Handle_TokenResolutionSucceeds_ShouldReturnExpiryFromSetting()
    {
        var command = new RefreshKeetaAccessTokenCommand(1, 2);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, true, Arg.Any<CancellationToken>()).Returns(Result.Success("new-token"));

        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        var expiresAt = DateTime.UtcNow.AddHours(2);
        setting.UpdateToken("new-token", expiresAt);
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_SettingNotFoundAfterRefresh_ShouldReturnNullExpiry()
    {
        var command = new RefreshKeetaAccessTokenCommand(1, 2);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, true, Arg.Any<CancellationToken>()).Returns(Result.Success("new-token"));
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenExpiresAtUtc.Should().BeNull();
    }
}
