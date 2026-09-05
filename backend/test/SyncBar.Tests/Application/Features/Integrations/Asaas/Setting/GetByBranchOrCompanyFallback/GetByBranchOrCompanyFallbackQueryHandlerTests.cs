using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;

public sealed class GetByBranchOrCompanyFallbackQueryHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByBranchOrCompanyFallbackQueryHandler _handler;

    public GetByBranchOrCompanyFallbackQueryHandlerTests()
    {
        _handler = new GetByBranchOrCompanyFallbackQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingResolved_ShouldReturnNotFound()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(new GetByBranchOrCompanyFallbackQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_ResolvedSettingIsInactive_ShouldReturnNotFound()
    {
        var setting = AsaasIntegrationSetting.Create(1, 2, "api-key", isActive: false).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetByBranchOrCompanyFallbackQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_BranchSpecificSettingResolved_ShouldReturnIt()
    {
        var setting = AsaasIntegrationSetting.Create(1, 2, "api-key").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetByBranchOrCompanyFallbackQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoBranchInformed_ShouldFallBackToCompanySetting()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetByBranchOrCompanyFallbackQuery(1, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().BeNull();
    }
}
