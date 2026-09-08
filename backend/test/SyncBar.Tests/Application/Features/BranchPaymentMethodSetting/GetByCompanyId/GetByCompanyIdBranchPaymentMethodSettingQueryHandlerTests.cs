using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetByCompanyId;

public sealed class GetByCompanyIdBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByCompanyIdBranchPaymentMethodSettingQueryHandler _handler;

    public GetByCompanyIdBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetByCompanyIdBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExists_ShouldReturnMappedResponse()
    {
        var setting = SettingEntity.Create(companyId: 1).Value;
        _settingRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(new GetByCompanyIdBranchPaymentMethodSettingQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnSuccessWithNull()
    {
        _settingRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(new GetByCompanyIdBranchPaymentMethodSettingQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
