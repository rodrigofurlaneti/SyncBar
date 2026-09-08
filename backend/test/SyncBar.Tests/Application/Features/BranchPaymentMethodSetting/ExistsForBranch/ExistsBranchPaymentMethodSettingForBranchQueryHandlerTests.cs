using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.ExistsForBranch;

public sealed class ExistsBranchPaymentMethodSettingForBranchQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsBranchPaymentMethodSettingForBranchQueryHandler _handler;

    public ExistsBranchPaymentMethodSettingForBranchQueryHandlerTests()
    {
        _handler = new ExistsBranchPaymentMethodSettingForBranchQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExistsForBranch_ShouldReturnTrue()
    {
        _settingRepository.ExistsForBranchAsync(2, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsBranchPaymentMethodSettingForBranchQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoSettingForBranch_ShouldReturnFalse()
    {
        _settingRepository.ExistsForBranchAsync(2, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsBranchPaymentMethodSettingForBranchQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
