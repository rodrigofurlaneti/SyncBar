using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood;

public sealed class TestIfoodConnectionCommandHandlerTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodAuthClient _authClient = Substitute.For<IIfoodAuthClient>();
    private readonly ISecretProtector _secretProtector = Substitute.For<ISecretProtector>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly TestIfoodConnectionCommandHandler _handler;

    public TestIfoodConnectionCommandHandlerTests()
    {
        _handler = new TestIfoodConnectionCommandHandler(_settingRepository, _authClient, _secretProtector, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingForCompany_ShouldReturnFailureMessageWithoutCallingAuthClient()
    {
        var command = new TestIfoodConnectionCommand(CompanyId: 1);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().NotBeNullOrEmpty();
        await _authClient.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingWithoutClientSecret_ShouldReturnFailureMessageWithoutCallingAuthClient()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-1", clientSecretEncrypted: null, enabled: false, ifoodCustomerId: null);

        var command = new TestIfoodConnectionCommand(CompanyId: 1);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        await _authClient.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnprotectThrows_ShouldReturnFailureMessageWithoutCallingAuthClient()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-1", clientSecretEncrypted: "corrupted", enabled: false, ifoodCustomerId: null);

        var command = new TestIfoodConnectionCommand(CompanyId: 1);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);
        _secretProtector.Unprotect(Arg.Any<string>(), "corrupted")
            .Returns(_ => throw new InvalidOperationException("key changed"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().NotBeNullOrEmpty();
        await _authClient.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuthSucceeds_ShouldRegisterConnectionTestPersistAndReturnSuccess()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-1", clientSecretEncrypted: "encrypted", enabled: true, ifoodCustomerId: null);

        var command = new TestIfoodConnectionCommand(CompanyId: 1);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(Success: true, AccessToken: "token-1", ExpiresInSeconds: 3600, ErrorMessage: null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.ErrorMessage.Should().BeNull();
        setting.LastConnectionTestSucceeded.Should().BeTrue();
        setting.LastConnectionTestAt.Should().NotBeNull();
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuthFails_ShouldRegisterConnectionTestFailureAndReturnErrorMessage()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-1", clientSecretEncrypted: "encrypted", enabled: true, ifoodCustomerId: null);

        var command = new TestIfoodConnectionCommand(CompanyId: 1);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);
        _secretProtector.Unprotect(Arg.Any<string>(), "encrypted").Returns("plain-secret");
        _authClient.AuthenticateAsync("client-1", "plain-secret", Arg.Any<CancellationToken>())
            .Returns(new IfoodAuthResult(Success: false, AccessToken: null, ExpiresInSeconds: null, ErrorMessage: "Invalid credentials"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("Invalid credentials");
        setting.LastConnectionTestSucceeded.Should().BeFalse();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
