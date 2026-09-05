using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate;

public sealed class GetAsaasSettingByBranchIdForUpdateQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasSettingByBranchIdForUpdateQueryHandler _handler;

    public GetAsaasSettingByBranchIdForUpdateQueryHandlerTests()
    {
        _handler = new GetAsaasSettingByBranchIdForUpdateQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingForBranch_ShouldReturnNotFound()
    {
        _settingRepository.GetByBranchIdForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(new GetAsaasSettingByBranchIdForUpdateQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFoundForBranch_ShouldReturnMappedResponse()
    {
        var setting = AsaasIntegrationSetting.Create(1, 2, "api-key").Value;
        _settingRepository.GetByBranchIdForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetAsaasSettingByBranchIdForUpdateQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().Be(2);
    }
}
