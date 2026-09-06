using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodTokenProviderTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly ISecretProtector _secretProtector = Substitute.For<ISecretProtector>();
    private readonly IIfoodAuthClient _authClient = Substitute.For<IIfoodAuthClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IfoodTokenProvider _provider;

    public IfoodTokenProviderTests()
    {
        _provider = new IfoodTokenProvider(_cache, _settingRepository, _secretProtector, _authClient, _logRepository, _unitOfWork);
    }

    private static IfoodIntegrationSetting EnabledSetting(long companyId = 1)
    {
        var setting = IfoodIntegrationSetting.Create(companyId).Value;
        setting.SaveCredentials("client-1", "encrypted-secret", enabled: true, ifoodCustomerId: null);
        return setting;
    }

    [Fact]
    public async Task GetAccessTokenAsync_CachedToken_ShouldReturnCachedWithoutCallingRepositoryOrAuthClient()
    {
        _cache.Set("Ifood:token:1", "cached-token");

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().Be("cached-token");
        await _settingRepository.DidNotReceive().GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _authClient.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoSetting_ShouldReturnNullAndLogFailure()
    {
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => !l.IsSuccess), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessTokenAsync_SettingDisabled_ShouldReturnNull()
    {
        var setting = IfoodIntegrationSetting.Create(1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: false, ifoodCustomerId: null);
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessTokenAsync_SettingMissingClientId_ShouldReturnNull()
    {
        var setting = IfoodIntegrationSetting.Create(1).Value;
        setting.SaveCredentials(null, "encrypted", enabled: true, ifoodCustomerId: null);
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessTokenAsync_SettingMissingEncryptedSecret_ShouldReturnNull()
    {
        var setting = IfoodIntegrationSetting.Create(1).Value;
        setting.SaveCredentials("client-1", null, enabled: true, ifoodCustomerId: null);
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessTokenAsync_UnprotectThrows_ShouldReturnNullAndLogFailure()
    {
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns(_ => throw new System.Security.Cryptography.CryptographicException("chave perdida"));

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.ErrorMessage!.Contains("chave perdida")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessTokenAsync_AuthenticationFails_ShouldReturnNullAndNotCache()
    {
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(false, null, null, "credenciais inválidas"));

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().BeNull();
        _cache.TryGetValue("Ifood:token:1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessTokenAsync_Success_ShouldCacheTokenAndReturnIt()
    {
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(true, "new-token", 3600, null));

        var token = await _provider.GetAccessTokenAsync(1, CancellationToken.None);

        token.Should().Be("new-token");
        _cache.TryGetValue<string>("Ifood:token:1", out var cached).Should().BeTrue();
        cached.Should().Be("new-token");
    }

    [Fact]
    public async Task Invalidate_ShouldRemoveCachedTokenForCompany()
    {
        _cache.Set("Ifood:token:1", "cached-token");

        _provider.Invalidate(1);

        _cache.TryGetValue("Ifood:token:1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithStopwatch_NoSetting_ShouldLogElapsedMillisecondsAndReturnNull()
    {
        var stopwatch = Stopwatch.StartNew();
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var token = await _provider.GetAccessTokenAsync(1, stopwatch, CancellationToken.None);

        token.Should().BeNull();
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.ExecutionTimeMs == stopwatch.ElapsedMilliseconds || l.ExecutionTimeMs >= 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithStopwatch_Success_ShouldCacheAndReturnToken()
    {
        var stopwatch = Stopwatch.StartNew();
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(true, "new-token", null, null));

        var token = await _provider.GetAccessTokenAsync(1, stopwatch, CancellationToken.None);

        token.Should().Be("new-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithStopwatch_UnprotectThrows_ShouldReturnNull()
    {
        var stopwatch = Stopwatch.StartNew();
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns(_ => throw new System.Security.Cryptography.CryptographicException("erro"));

        var token = await _provider.GetAccessTokenAsync(1, stopwatch, CancellationToken.None);

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithStopwatch_CachedToken_ShouldReturnCached()
    {
        _cache.Set("Ifood:token:1", "cached-token");
        var stopwatch = Stopwatch.StartNew();

        var token = await _provider.GetAccessTokenAsync(1, stopwatch, CancellationToken.None);

        token.Should().Be("cached-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithStopwatch_AuthenticationFails_ShouldReturnNull()
    {
        var stopwatch = Stopwatch.StartNew();
        _settingRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(EnabledSetting());
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted-secret").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(false, null, null, "erro"));

        var token = await _provider.GetAccessTokenAsync(1, stopwatch, CancellationToken.None);

        token.Should().BeNull();
    }
}
