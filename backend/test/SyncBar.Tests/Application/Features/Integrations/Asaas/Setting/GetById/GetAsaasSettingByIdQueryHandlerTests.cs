using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetById;

public sealed class GetAsaasSettingByIdQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasSettingByIdQueryHandler _handler;

    public GetAsaasSettingByIdQueryHandlerTests()
    {
        _handler = new GetAsaasSettingByIdQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFound()
    {
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(new GetAsaasSettingByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingFound_ShouldReturnMappedResponse()
    {
        var setting = AsaasIntegrationSetting.Create(1, 2, "api-key", environment: "Production").Value;
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetAsaasSettingByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.Environment.Should().Be("Production");
        result.Value.IsActive.Should().BeTrue();
    }
}
