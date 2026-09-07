using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization;

public sealed class KeetaAccessTokenProviderTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly IKeetaAuthClient _authClient = Substitute.For<IKeetaAuthClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IKeetaAccessTokenProvider _provider;

    public KeetaAccessTokenProviderTests()
    {
        _provider = new KeetaAccessTokenProvider(_settingRepository, _authClient, _unitOfWork);
    }

    private static KeetaIntegrationSetting CreateSetting(long companyId = 1, long branchId = 2) =>
        KeetaIntegrationSetting.Create(companyId, branchId).Value;

    [Fact]
    public async Task GetValidAccessTokenAsync_SettingNotFound_ReturnsFailure()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns((KeetaIntegrationSetting?)null);

        var result = await _provider.GetValidAccessTokenAsync(1, 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.SettingNotConfigured");
        await _authClient.DidNotReceive().GetAccessTokenAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ForceRefreshTrueEvenWithValidToken_RefreshesToken()
    {
        var setting = CreateSetting();
        setting.UpdateToken("cached-token", DateTime.UtcNow.AddHours(1));
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);
        _authClient.GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaTokenResponse("new-token", "Bearer", 3600));

        var result = await _provider.GetValidAccessTokenAsync(1, 2, forceRefresh: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("new-token");
        setting.CurrentAccessToken.Should().Be("new-token");
        await _authClient.Received(1).GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>());
        _settingRepository.Received(1).Update(setting);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_CurrentAccessTokenNullOrWhiteSpace_RefreshesToken()
    {
        var setting = CreateSetting();
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);
        _authClient.GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaTokenResponse("new-token", "Bearer", 3600));

        var result = await _provider.GetValidAccessTokenAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("new-token");
        await _authClient.Received(1).GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_TokenExpiresAtUtcNull_RefreshesToken()
    {
        // KeetaIntegrationSetting.UpdateToken always sets CurrentAccessToken and
        // TokenExpiresAtUtc together, so a freshly created setting (never updated) is the only
        // reachable state, via the public API, where TokenExpiresAtUtc is null — it also has a
        // null CurrentAccessToken, so this exercises the "TokenExpiresAtUtc.HasValue" condition
        // of the AND-chain short-circuiting the same way the empty-token test does.
        var setting = CreateSetting();
        setting.TokenExpiresAtUtc.Should().BeNull();
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);
        _authClient.GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaTokenResponse("new-token", "Bearer", 3600));

        var result = await _provider.GetValidAccessTokenAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("new-token");
        await _authClient.Received(1).GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ExpiryInsideTwoMinuteMargin_RefreshesToken()
    {
        var setting = CreateSetting();
        setting.UpdateToken("cached-token", DateTime.UtcNow.AddMinutes(1));
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);
        _authClient.GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaTokenResponse("new-token", "Bearer", 3600));

        var result = await _provider.GetValidAccessTokenAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("new-token");
        await _authClient.Received(1).GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ExpiryOutsideMargin_ReturnsCachedTokenWithoutCallingAuthClient()
    {
        var setting = CreateSetting();
        setting.UpdateToken("cached-token", DateTime.UtcNow.AddMinutes(10));
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _provider.GetValidAccessTokenAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("cached-token");
        await _authClient.DidNotReceive().GetAccessTokenAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        _settingRepository.DidNotReceive().Update(Arg.Any<KeetaIntegrationSetting>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_RefreshSuccessPath_UpdatesSettingPersistsAndReturnsNewToken()
    {
        var setting = CreateSetting();
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);
        _authClient.GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaTokenResponse("brand-new-token", "Bearer", 1800));

        var beforeCall = DateTime.UtcNow;
        var result = await _provider.GetValidAccessTokenAsync(1, 2);
        var afterCall = DateTime.UtcNow;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("brand-new-token");
        setting.CurrentAccessToken.Should().Be("brand-new-token");
        setting.TokenExpiresAtUtc.Should().NotBeNull();
        setting.TokenExpiresAtUtc!.Value.Should().BeOnOrAfter(beforeCall.AddSeconds(1800))
            .And.BeOnOrBefore(afterCall.AddSeconds(1800).AddSeconds(1));
        await _authClient.Received(1).GetAccessTokenAsync(1, 2, Arg.Any<CancellationToken>());
        _settingRepository.Received(1).Update(setting);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
