using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;

public sealed class GetKeetaSettingByBranchOrCompanyFallbackQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByBranchOrCompanyFallbackQueryHandler _handler;

    public GetKeetaSettingByBranchOrCompanyFallbackQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByBranchOrCompanyFallbackQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingForEitherScope_ShouldReturnNotFound()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByBranchOrCompanyFallbackQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByBranchOrCompanyFallbackQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NullBranchId_ShouldQueryWithNullBranch()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByBranchOrCompanyFallbackQuery(1, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _settingRepository.Received(1).GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>());
    }
}
