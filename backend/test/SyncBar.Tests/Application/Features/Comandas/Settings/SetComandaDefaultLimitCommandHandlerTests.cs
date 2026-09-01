using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Comandas.Settings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Comandas.Settings;

public sealed class SetComandaDefaultLimitCommandHandlerTests
{
    private readonly IComandaSettingRepository _settingRepository = Substitute.For<IComandaSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetComandaDefaultLimitCommandHandler _handler;

    public SetComandaDefaultLimitCommandHandlerTests()
    {
        _handler = new SetComandaDefaultLimitCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoExistingSettingAndInvalidAmount_ShouldReturnFailureWithoutPersisting()
    {
        var command = new SetComandaDefaultLimitCommand(BranchId: 1, DefaultLimitAmount: 0m);
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((ComandaSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<ComandaSetting>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingSetting_ShouldCreateAndPersistNewSetting()
    {
        var command = new SetComandaDefaultLimitCommand(BranchId: 1, DefaultLimitAmount: 200m);
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((ComandaSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<ComandaSetting>(s => s.BranchId == command.BranchId && s.DefaultLimitAmount == command.DefaultLimitAmount),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSettingAndInvalidAmount_ShouldReturnFailureWithoutChangingSetting()
    {
        var existing = ComandaSetting.Create(1, 100m).Value;
        var command = new SetComandaDefaultLimitCommand(BranchId: 1, DefaultLimitAmount: -5m);
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        existing.DefaultLimitAmount.Should().Be(100m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSetting_ShouldUpdateItsLimit()
    {
        var existing = ComandaSetting.Create(1, 100m).Value;
        var command = new SetComandaDefaultLimitCommand(BranchId: 1, DefaultLimitAmount: 300m);
        _settingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.DefaultLimitAmount.Should().Be(300m);
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<ComandaSetting>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
