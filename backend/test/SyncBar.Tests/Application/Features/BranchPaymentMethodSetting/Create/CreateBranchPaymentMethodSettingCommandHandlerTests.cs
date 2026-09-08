using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Create;
using SyncBar.Domain.Repositories;
using Xunit;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.Create;

public sealed class CreateBranchPaymentMethodSettingCommandHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateBranchPaymentMethodSettingCommandHandler _handler;

    public CreateBranchPaymentMethodSettingCommandHandlerTests()
    {
        _handler = new CreateBranchPaymentMethodSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoExistingSettingForScope_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateBranchPaymentMethodSettingCommand(
            CompanyId: 1, BranchId: 2, EnablePix: true, EnableBoleto: false,
            EnableCreditCard: false, EnableDebitCard: false, EnableCashMachine: true, IsActive: true);
        _settingRepository.GetByScopeAsync(command.CompanyId, command.BranchId, Arg.Any<CancellationToken>())
            .Returns((SettingEntity?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.EnablePix.Should().BeTrue();
        result.Value.EnableBoleto.Should().BeFalse();
        result.Value.EnableCreditCard.Should().BeFalse();
        result.Value.EnableDebitCard.Should().BeFalse();
        result.Value.EnableCashMachine.Should().BeTrue();

        await _settingRepository.Received(1).AddAsync(
            Arg.Is<SettingEntity>(s => s.CompanyId == 1 && s.BranchId == 2), Arg.Any<CancellationToken>());
        // Commit explicito do handler + commit do finally da base (log de auditoria).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSettingForBranchScope_ShouldReturnConflictFailureWithoutPersisting()
    {
        var command = new CreateBranchPaymentMethodSettingCommand(CompanyId: 1, BranchId: 2);
        var existing = SettingEntity.Create(1, 2).Value;
        _settingRepository.GetByScopeAsync(command.CompanyId, command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.AlreadyExists");
        result.Error.Message.Should().Contain("filial 2");
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSettingForCompanyLevelScope_ShouldReturnConflictFailureMentioningCompany()
    {
        var command = new CreateBranchPaymentMethodSettingCommand(CompanyId: 1, BranchId: null);
        var existing = SettingEntity.Create(1).Value;
        _settingRepository.GetByScopeAsync(command.CompanyId, command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BranchPaymentMethodSetting.AlreadyExists");
        result.Error.Message.Should().Contain("global para a empresa 1");
    }

    [Fact]
    public async Task Handle_InvalidDomainArguments_ShouldPropagateDomainFailureWithoutPersisting()
    {
        // CompanyId=0 nao passaria pelo validator em producao, mas o handler tambem precisa
        // propagar corretamente uma falha vinda da factory do dominio.
        var command = new CreateBranchPaymentMethodSettingCommand(CompanyId: 0, BranchId: null);
        _settingRepository.GetByScopeAsync(command.CompanyId, command.BranchId, Arg.Any<CancellationToken>())
            .Returns((SettingEntity?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanyId.Invalid");
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>(), Arg.Any<CancellationToken>());
        // Sem commit explicito nesse ramo: so o commit do finally da base (grava o log de auditoria).
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
