using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.ServiceFeeSetting;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainServiceFeeSetting = SyncBar.Domain.Entities.ServiceFeeSetting;

namespace SyncBar.Tests.Application.Features.Orders.ServiceFeeSetting;

public sealed class SetServiceFeeEnabledCommandHandlerTests
{
    private readonly IServiceFeeSettingRepository _settingRepository = Substitute.For<IServiceFeeSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetServiceFeeEnabledCommandHandler _handler;

    public SetServiceFeeEnabledCommandHandlerTests()
    {
        _handler = new SetServiceFeeEnabledCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoExistingSetting_ShouldCreateAndAddNewSetting()
    {
        var command = new SetServiceFeeEnabledCommand(BranchId: 1, Enabled: false);
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<DomainServiceFeeSetting>(s => s.BranchId == command.BranchId && s.Enabled == command.Enabled),
            Arg.Any<CancellationToken>());
        // Cria + salva: 1 commit explícito do handler + 1 do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSetting_ShouldUpdateWithoutAddingNewOne()
    {
        var command = new SetServiceFeeEnabledCommand(BranchId: 1, Enabled: false);
        var existingSetting = DomainServiceFeeSetting.Create(command.BranchId, true).Value;
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existingSetting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingSetting.Enabled.Should().BeFalse();
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<DomainServiceFeeSetting>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TogglingEnabledTrueToFalse_ShouldSucceedAndPersistFalse()
    {
        var command = new SetServiceFeeEnabledCommand(BranchId: 1, Enabled: false);
        var existingSetting = DomainServiceFeeSetting.Create(command.BranchId, true).Value;
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existingSetting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingSetting.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TogglingEnabledFalseToTrue_ShouldSucceedAndPersistTrue()
    {
        var command = new SetServiceFeeEnabledCommand(BranchId: 1, Enabled: true);
        var existingSetting = DomainServiceFeeSetting.Create(command.BranchId, false).Value;
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existingSetting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingSetting.Enabled.Should().BeTrue();
    }
}
