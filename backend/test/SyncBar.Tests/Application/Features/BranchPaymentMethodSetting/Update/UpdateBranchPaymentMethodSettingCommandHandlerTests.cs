using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Update;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.Update;

public sealed class UpdateBranchPaymentMethodSettingCommandHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateBranchPaymentMethodSettingCommandHandler _handler;

    public UpdateBranchPaymentMethodSettingCommandHandlerTests()
    {
        _handler = new UpdateBranchPaymentMethodSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFoundFailure()
    {
        var command = new UpdateBranchPaymentMethodSettingCommand(Id: 1, CompanyId: 1, EnablePix: false);
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.NotFound");
        _settingRepository.DidNotReceive().Update(Arg.Any<SettingEntity>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingBelongsToDifferentCompany_ShouldReturnNotFoundFailure()
    {
        var command = new UpdateBranchPaymentMethodSettingCommand(Id: 1, CompanyId: 99, EnablePix: false);
        var setting = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.NotFound");
        _settingRepository.DidNotReceive().Update(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateOnlyProvidedFlagsAndPersist()
    {
        var command = new UpdateBranchPaymentMethodSettingCommand(
            Id: 1, CompanyId: 1, EnablePix: false, EnableCashMachine: true);
        var setting = SettingEntity.Create(companyId: 1, branchId: 2, enablePix: true, enableCashMachine: false).Value;
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        setting.EnablePix.Should().BeFalse();
        setting.EnableCashMachine.Should().BeTrue();
        setting.EnableBoleto.Should().BeTrue(); // nao informado no comando: permanece no valor default de Create
        _settingRepository.Received(1).Update(setting);
        // Commit explicito do handler + commit do finally da base (log de auditoria).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
