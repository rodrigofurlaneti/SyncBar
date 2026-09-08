using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetAllActive;

public sealed class GetAllActiveBranchPaymentMethodSettingQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllActiveBranchPaymentMethodSettingQueryHandler _handler;

    public GetAllActiveBranchPaymentMethodSettingQueryHandlerTests()
    {
        _handler = new GetAllActiveBranchPaymentMethodSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MultipleActiveSettings_ShouldReturnAllMapped()
    {
        var first = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        var second = SettingEntity.Create(companyId: 3, branchId: null).Value;
        _settingRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<SettingEntity>)[first, second]);

        var result = await _handler.Handle(new GetAllActiveBranchPaymentMethodSettingQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(r => r.CompanyId == 1 && r.BranchId == 2);
        result.Value.Should().Contain(r => r.CompanyId == 3 && r.BranchId == null);
    }

    [Fact]
    public async Task Handle_NoActiveSettings_ShouldReturnEmptyList()
    {
        _settingRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<SettingEntity>)Array.Empty<SettingEntity>());

        var result = await _handler.Handle(new GetAllActiveBranchPaymentMethodSettingQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
