using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback;

public sealed class GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler _handler;

    public GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BranchHasOwnSetting_ShouldReturnBranchSetting()
    {
        var query = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(1, 2);
        var branchSetting = SettingEntity.Create(companyId: 1, branchId: 2, enablePix: false).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 2, Arg.Any<CancellationToken>()).Returns(branchSetting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BranchId.Should().Be(2);
        result.Value.EnablePix.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_BranchHasNoSettingButCompanyDoes_ShouldReturnCompanySetting()
    {
        var query = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(1, 3);
        var companySetting = SettingEntity.Create(companyId: 1, branchId: null, enablePix: true).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 3, Arg.Any<CancellationToken>()).Returns(companySetting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BranchId.Should().BeNull();
        result.Value.CompanyId.Should().Be(1);
        result.Value.EnablePix.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NeitherBranchNorCompanyHasSetting_ShouldReturnSuccessWithNull()
    {
        var query = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(1, 3);
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 3, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
