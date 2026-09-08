using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Delete;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.Delete;

public sealed class DeleteBranchPaymentMethodSettingCommandHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteBranchPaymentMethodSettingCommandHandler _handler;

    public DeleteBranchPaymentMethodSettingCommandHandlerTests()
    {
        _handler = new DeleteBranchPaymentMethodSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnNotFoundFailure()
    {
        var command = new DeleteBranchPaymentMethodSettingCommand(Id: 1, CompanyId: 1);
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns((SettingEntity?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.NotFound");
        _settingRepository.DidNotReceive().Delete(Arg.Any<SettingEntity>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingBelongsToDifferentCompany_ShouldReturnNotFoundFailureWithoutDeleting()
    {
        var command = new DeleteBranchPaymentMethodSettingCommand(Id: 1, CompanyId: 99);
        var setting = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.NotFound");
        _settingRepository.DidNotReceive().Delete(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var command = new DeleteBranchPaymentMethodSettingCommand(Id: 1, CompanyId: 1);
        var setting = SettingEntity.Create(companyId: 1, branchId: 2).Value;
        _settingRepository.GetByIdForUpdateAsync(command.Id, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _settingRepository.Received(1).Delete(setting);
        // Commit explicito do handler + commit do finally da base (log de auditoria).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
