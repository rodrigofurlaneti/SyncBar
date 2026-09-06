using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.Update;

public sealed class UpdateKeetaIntegrationSettingCommandHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationSettingCommandHandler _handler;

    public UpdateKeetaIntegrationSettingCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationSettingCommand(1, 1, "client-id");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        var command = new UpdateKeetaIntegrationSettingCommand(1, 2, "client-id");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldSaveCredentialsAndCommit()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        var command = new UpdateKeetaIntegrationSettingCommand(1, 1, "new-client-id", "new-client-secret", "new-app-id", "https://new.keeta/api");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        setting.ClientId.Should().Be("new-client-id");
        setting.ClientSecret.Should().Be("new-client-secret");
        setting.AppId.Should().Be("new-app-id");
        setting.BaseUrl.Should().Be("https://new.keeta/api");
        _settingRepository.Received(1).Update(setting);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldKeepExistingValuesForOmittedFields()
    {
        var setting = KeetaIntegrationSetting.Create(1, 0).Value;
        setting.SaveCredentials("original-client-id", "original-secret", "original-app-id");
        var command = new UpdateKeetaIntegrationSettingCommand(1, 1, ClientSecret: "updated-secret");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        setting.ClientId.Should().Be("original-client-id");
        setting.ClientSecret.Should().Be("updated-secret");
        setting.AppId.Should().Be("original-app-id");
    }
}
