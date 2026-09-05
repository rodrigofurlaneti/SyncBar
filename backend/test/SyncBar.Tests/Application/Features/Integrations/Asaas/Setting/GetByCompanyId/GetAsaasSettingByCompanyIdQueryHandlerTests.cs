using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;

public sealed class GetAsaasSettingByCompanyIdQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasSettingByCompanyIdQueryHandler _handler;

    public GetAsaasSettingByCompanyIdQueryHandlerTests()
    {
        _handler = new GetAsaasSettingByCompanyIdQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoGlobalSettingForCompany_ShouldReturnNotFound()
    {
        _settingRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(new GetAsaasSettingByCompanyIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_GlobalSettingFound_ShouldReturnMappedResponse()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;
        _settingRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetAsaasSettingByCompanyIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().BeNull();
    }
}
