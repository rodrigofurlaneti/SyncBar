using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.ServiceFeeSetting;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainServiceFeeSetting = SyncBar.Domain.Entities.ServiceFeeSetting;

namespace SyncBar.Tests.Application.Features.Orders.ServiceFeeSetting;

public sealed class GetServiceFeeSettingQueryHandlerTests
{
    private readonly IServiceFeeSettingRepository _settingRepository = Substitute.For<IServiceFeeSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetServiceFeeSettingQueryHandler _handler;

    public GetServiceFeeSettingQueryHandlerTests()
    {
        _handler = new GetServiceFeeSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingConfigured_ShouldReturnEnabledTrueByDefault()
    {
        var query = new GetServiceFeeSettingQuery(BranchId: 1);
        _settingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingExplicitlyEnabled_ShouldReturnEnabledTrue()
    {
        var query = new GetServiceFeeSettingQuery(BranchId: 1);
        var setting = DomainServiceFeeSetting.Create(query.BranchId, true).Value;
        _settingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SettingExplicitlyDisabled_ShouldReturnEnabledFalse()
    {
        var query = new GetServiceFeeSettingQuery(BranchId: 1);
        var setting = DomainServiceFeeSetting.Create(query.BranchId, false).Value;
        _settingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeFalse();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
