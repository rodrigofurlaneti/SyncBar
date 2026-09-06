using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByScope;

public sealed class GetKeetaSettingByScopeQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByScopeQueryHandler _handler;

    public GetKeetaSettingByScopeQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByScopeQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingForScope_ShouldReturnNotFound()
    {
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByScopeQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByScopeQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
    }
}
