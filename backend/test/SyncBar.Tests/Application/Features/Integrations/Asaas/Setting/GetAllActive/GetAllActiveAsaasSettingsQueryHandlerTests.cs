using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetAllActive;

public sealed class GetAllActiveAsaasSettingsQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllActiveAsaasSettingsQueryHandler _handler;

    public GetAllActiveAsaasSettingsQueryHandlerTests()
    {
        _handler = new GetAllActiveAsaasSettingsQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoActiveSettings_ShouldReturnEmptyList()
    {
        _settingRepository.GetAllActiveByCompanyIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationSetting>());

        var result = await _handler.Handle(new GetAllActiveAsaasSettingsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasActiveSettings_ShouldReturnMappedList()
    {
        var settings = new List<AsaasIntegrationSetting>
        {
            AsaasIntegrationSetting.Create(1, null, "api-key-1").Value,
            AsaasIntegrationSetting.Create(1, 2, "api-key-2").Value,
        };
        _settingRepository.GetAllActiveByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(settings);

        var result = await _handler.Handle(new GetAllActiveAsaasSettingsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.BranchId == null);
        result.Value.Should().Contain(s => s.BranchId == 2);
    }
}
