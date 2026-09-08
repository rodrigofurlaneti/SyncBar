using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId;

public sealed class GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandler _handler;

    public GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_CompanyHasActiveSettings_ShouldReturnAllMapped()
    {
        var branchSetting = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        var companySetting = SettingEntity.Create(companyId: 1, branchId: null).Value;
        _settingRepository.GetAllActiveByCompanyIdAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SettingEntity>)[branchSetting, companySetting]);

        var result = await _handler.Handle(new GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(r => r.CompanyId == 1);
    }

    [Fact]
    public async Task Handle_CompanyHasNoActiveSettings_ShouldReturnEmptyList()
    {
        _settingRepository.GetAllActiveByCompanyIdAsync(1, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SettingEntity>)Array.Empty<SettingEntity>());

        var result = await _handler.Handle(new GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
