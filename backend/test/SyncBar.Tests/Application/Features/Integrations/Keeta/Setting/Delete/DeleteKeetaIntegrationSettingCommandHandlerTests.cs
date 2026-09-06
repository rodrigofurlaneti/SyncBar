using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.Delete;

public sealed class DeleteKeetaIntegrationSettingCommandHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteKeetaIntegrationSettingCommandHandler _handler;

    public DeleteKeetaIntegrationSettingCommandHandlerTests()
    {
        _handler = new DeleteKeetaIntegrationSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteKeetaIntegrationSettingCommand(1, 1);
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        var command = new DeleteKeetaIntegrationSettingCommand(1, 2);
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
        _settingRepository.DidNotReceive().Delete(Arg.Any<KeetaIntegrationSetting>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        var command = new DeleteKeetaIntegrationSettingCommand(1, 1);
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _settingRepository.Received(1).Delete(setting);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
