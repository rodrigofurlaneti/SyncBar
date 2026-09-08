using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByScope;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetByScope;

public sealed class GetByScopeBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByScopeBranchPaymentMethodSettingQueryHandler _handler;

    public GetByScopeBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetByScopeBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExistsForScope_ShouldReturnMappedResponse()
    {
        var query = new GetByScopeBranchPaymentMethodSettingQuery(1, 2);
        var setting = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BranchId.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoSettingForScope_ShouldReturnSuccessWithNull()
    {
        var query = new GetByScopeBranchPaymentMethodSettingQuery(1, null);
        _settingRepository.GetByScopeAsync(1, null, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
