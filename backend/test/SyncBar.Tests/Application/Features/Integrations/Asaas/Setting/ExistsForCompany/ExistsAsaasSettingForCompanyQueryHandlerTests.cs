using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;

public sealed class ExistsAsaasSettingForCompanyQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsAsaasSettingForCompanyQueryHandler _handler;

    public ExistsAsaasSettingForCompanyQueryHandlerTests()
    {
        _handler = new ExistsAsaasSettingForCompanyQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExists_ShouldReturnTrue()
    {
        _settingRepository.ExistsForCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsAsaasSettingForCompanyQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SettingDoesNotExist_ShouldReturnFalse()
    {
        _settingRepository.ExistsForCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsAsaasSettingForCompanyQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
