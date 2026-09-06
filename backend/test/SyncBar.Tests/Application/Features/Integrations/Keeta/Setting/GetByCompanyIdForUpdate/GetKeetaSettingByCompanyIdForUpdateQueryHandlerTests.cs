using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate;

public sealed class GetKeetaSettingByCompanyIdForUpdateQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByCompanyIdForUpdateQueryHandler _handler;

    public GetKeetaSettingByCompanyIdForUpdateQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByCompanyIdForUpdateQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        _settingRepository.GetByCompanyIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByCompanyIdForUpdateQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        _settingRepository.GetByCompanyIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByCompanyIdForUpdateQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
    }
}
