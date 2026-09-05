using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.Update;

public sealed class UpdateAsaasIntegrationSettingCommandHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateAsaasIntegrationSettingCommandHandler _handler;

    public UpdateAsaasIntegrationSettingCommandHandlerTests()
    {
        _handler = new UpdateAsaasIntegrationSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateAsaasIntegrationSettingCommand(1, 1, "new-key");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;
        var command = new UpdateAsaasIntegrationSettingCommand(1, 2, "new-key");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
        _settingRepository.DidNotReceive().Update(Arg.Any<AsaasIntegrationSetting>());
    }

    [Fact]
    public async Task Handle_EmptyApiKeyProvided_ShouldReturnDomainValidationFailure()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;
        var command = new UpdateAsaasIntegrationSettingCommand(1, 1, "   ");
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiKey.Empty");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateAndCommit()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "old-key", environment: "Sandbox").Value;
        var command = new UpdateAsaasIntegrationSettingCommand(1, 1, "new-key", "new-webhook", "Production", false);
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        setting.ApiKeyEncrypted.Should().Be("new-key");
        setting.Environment.Should().Be("Production");
        setting.IsActive.Should().BeFalse();
        _settingRepository.Received(1).Update(setting);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_ShouldKeepExistingValues()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "old-key", environment: "Sandbox").Value;
        var command = new UpdateAsaasIntegrationSettingCommand(1, 1);
        _settingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        setting.ApiKeyEncrypted.Should().Be("old-key");
        setting.Environment.Should().Be("Sandbox");
    }
}
