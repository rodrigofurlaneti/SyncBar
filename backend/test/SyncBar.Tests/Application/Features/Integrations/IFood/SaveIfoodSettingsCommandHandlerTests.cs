using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood;

public sealed class SaveIfoodSettingsCommandHandlerTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly ISecretProtector _secretProtector = Substitute.For<ISecretProtector>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SaveIfoodSettingsCommandHandler _handler;

    public SaveIfoodSettingsCommandHandlerTests()
    {
        _handler = new SaveIfoodSettingsCommandHandler(_settingRepository, _secretProtector, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoExistingSetting_ShouldCreateNewSettingEncryptSecretAndPersist()
    {
        var command = new SaveIfoodSettingsCommand(CompanyId: 1, ClientId: "client-1", ClientSecret: "my-secret", Enabled: true, IfoodCustomerId: "cust-1");
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns((IfoodIntegrationSetting?)null);
        _secretProtector.Protect(Arg.Any<string>(), "my-secret").Returns("encrypted-my-secret");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<IfoodIntegrationSetting>(s =>
                s.CompanyId == command.CompanyId &&
                s.ClientId == "client-1" &&
                s.ClientSecretEncrypted == "encrypted-my-secret" &&
                s.Enabled &&
                s.IfoodCustomerId == "cust-1"),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSetting_ShouldUpdateInPlaceWithoutCreatingNew()
    {
        var existing = IfoodIntegrationSetting.Create(companyId: 1).Value;
        existing.SaveCredentials("old-client", "old-encrypted", enabled: false, ifoodCustomerId: null);

        var command = new SaveIfoodSettingsCommand(CompanyId: 1, ClientId: "new-client", ClientSecret: "new-secret", Enabled: true, IfoodCustomerId: "cust-2");
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(existing);
        _secretProtector.Protect(Arg.Any<string>(), "new-secret").Returns("encrypted-new");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.ClientId.Should().Be("new-client");
        existing.ClientSecretEncrypted.Should().Be("encrypted-new");
        existing.Enabled.Should().BeTrue();
        existing.IfoodCustomerId.Should().Be("cust-2");
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<IfoodIntegrationSetting>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankClientSecret_ShouldKeepPreviouslySavedSecretAndNotCallProtector()
    {
        var existing = IfoodIntegrationSetting.Create(companyId: 1).Value;
        existing.SaveCredentials("old-client", "old-encrypted", enabled: true, ifoodCustomerId: null);

        // Em branco = "manter o segredo já salvo" — o frontend nunca reexibe o valor salvo.
        var command = new SaveIfoodSettingsCommand(CompanyId: 1, ClientId: "old-client", ClientSecret: null, Enabled: true);
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.ClientSecretEncrypted.Should().Be("old-encrypted");
        _secretProtector.DidNotReceive().Protect(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhitespaceIfoodCustomerId_ShouldNormalizeToNullBeforePersisting()
    {
        var command = new SaveIfoodSettingsCommand(CompanyId: 1, ClientId: "client-1", ClientSecret: null, Enabled: false, IfoodCustomerId: "   ");
        _settingRepository.GetByCompanyForUpdateAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<IfoodIntegrationSetting>(s => s.IfoodCustomerId == null),
            Arg.Any<CancellationToken>());
    }
}
