using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood;

public sealed class GetIfoodSettingsQueryHandlerTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodSettingsQueryHandler _handler;

    public GetIfoodSettingsQueryHandlerTests()
    {
        _handler = new GetIfoodSettingsQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingForCompany_ShouldReturnDefaultDisabledResponse()
    {
        var query = new GetIfoodSettingsQuery(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeFalse();
        result.Value.ClientId.Should().BeNull();
        result.Value.Enabled.Should().BeFalse();
        result.Value.LastConnectionTestAt.Should().BeNull();
        result.Value.LastConnectionTestSucceeded.Should().BeNull();
        result.Value.IfoodCustomerId.Should().BeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingWithoutClientSecret_ShouldReturnHasCredentialsFalse()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-abc", clientSecretEncrypted: null, enabled: false, ifoodCustomerId: null);

        var query = new GetIfoodSettingsQuery(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeFalse();
        result.Value.ClientId.Should().Be("client-abc");
    }

    [Fact]
    public async Task Handle_SettingWithFullCredentials_ShouldReturnHasCredentialsTrueAndMappedFields()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials(clientId: "client-abc", clientSecretEncrypted: "encrypted-secret", enabled: true, ifoodCustomerId: "cust-1");
        setting.RegisterConnectionTest(succeeded: true);

        var query = new GetIfoodSettingsQuery(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();
        result.Value.ClientId.Should().Be("client-abc");
        result.Value.Enabled.Should().BeTrue();
        result.Value.LastConnectionTestAt.Should().Be(setting.LastConnectionTestAt);
        result.Value.LastConnectionTestSucceeded.Should().BeTrue();
        result.Value.IfoodCustomerId.Should().Be("cust-1");
    }
}
