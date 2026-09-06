using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate;

public sealed class GetKeetaSettingByIdForUpdateQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaSettingByIdForUpdateQueryHandler _handler;

    public GetKeetaSettingByIdForUpdateQueryHandlerTests()
    {
        _handler = new GetKeetaSettingByIdForUpdateQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(new GetKeetaSettingByIdForUpdateQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = KeetaIntegrationSetting.Create(1, 2).Value;
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetKeetaSettingByIdForUpdateQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
    }
}
