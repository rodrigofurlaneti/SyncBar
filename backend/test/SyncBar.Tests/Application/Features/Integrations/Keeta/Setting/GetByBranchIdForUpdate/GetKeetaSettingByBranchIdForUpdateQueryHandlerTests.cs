using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate;

public sealed class GetKeetaSettingByBranchIdForUpdateQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByBranchIdForUpdateQueryHandler _handler;

    public GetKeetaSettingByBranchIdForUpdateQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByBranchIdForUpdateQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        _settingRepository.GetByBranchIdForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByBranchIdForUpdateQuery(2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        _settingRepository.GetByBranchIdForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByBranchIdForUpdateQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().Be(2);
    }
}
