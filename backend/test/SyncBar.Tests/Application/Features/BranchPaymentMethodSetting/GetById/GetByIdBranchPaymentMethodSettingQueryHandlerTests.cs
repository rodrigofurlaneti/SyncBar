using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetById;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetById;

public sealed class GetByIdBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByIdBranchPaymentMethodSettingQueryHandler _handler;

    public GetByIdBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetByIdBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExists_ShouldReturnMappedResponse()
    {
        var setting = SettingEntity.Create(companyId: 1, branchId: 2, enablePix: false).Value;
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetByIdBranchPaymentMethodSettingQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.EnablePix.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnSuccessWithNull()
    {
        _settingRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(new GetByIdBranchPaymentMethodSettingQuery(999), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
