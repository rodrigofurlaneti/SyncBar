using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.SetTarget;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.SetTarget;

public sealed class SetRevenueTargetCommandHandlerTests
{
    private readonly IRevenueTargetRepository _targetRepository = Substitute.For<IRevenueTargetRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetRevenueTargetCommandHandler _handler;

    public SetRevenueTargetCommandHandlerTests()
    {
        _handler = new SetRevenueTargetCommandHandler(_targetRepository, _logRepository, _unitOfWork);
    }

    private static SetRevenueTargetCommand CreateCommand(decimal targetAmount = 10000m)
        => new(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 9, TargetAmount: targetAmount);

    private static RevenueTarget CreateExistingTarget(decimal targetAmount = 8000m)
        => RevenueTarget.Create(branchId: 1, referenceYear: 2026, referenceMonth: 9, targetAmount: targetAmount).Value;

    [Fact]
    public async Task Handle_NoExistingTargetAndInvalidAmount_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(targetAmount: 0m);
        _targetRepository.GetByBranchAndMonthForUpdateAsync(
            command.BranchId, command.ReferenceYear, command.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns((RevenueTarget?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RevenueTarget.InvalidAmount");
        await _targetRepository.DidNotReceive().AddAsync(Arg.Any<RevenueTarget>(), Arg.Any<CancellationToken>());
        // Só o commit do finally da base — nenhum commit explícito nesse caminho.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingTarget_ShouldCreateNewTargetAndReturnItsId()
    {
        var command = CreateCommand(targetAmount: 10000m);
        _targetRepository.GetByBranchAndMonthForUpdateAsync(
            command.BranchId, command.ReferenceYear, command.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns((RevenueTarget?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _targetRepository.Received(1).AddAsync(
            Arg.Is<RevenueTarget>(t =>
                t.BranchId == command.BranchId &&
                t.ReferenceYear == command.ReferenceYear &&
                t.ReferenceMonth == command.ReferenceMonth &&
                t.TargetAmount == command.TargetAmount),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo (branch de criação) + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTargetAndInvalidAmount_ShouldReturnFailureWithoutPersisting()
    {
        var existing = CreateExistingTarget();
        var command = CreateCommand(targetAmount: -1m);
        _targetRepository.GetByBranchAndMonthForUpdateAsync(
            command.BranchId, command.ReferenceYear, command.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RevenueTarget.InvalidAmount");
        // O valor antigo permanece intacto — UpdateAmount falhou antes de qualquer mutação persistente.
        existing.TargetAmount.Should().Be(8000m);
        await _targetRepository.DidNotReceive().AddAsync(Arg.Any<RevenueTarget>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTarget_ShouldUpdateAmountInPlaceAndReturnExistingId()
    {
        var existing = CreateExistingTarget(targetAmount: 8000m);
        var command = CreateCommand(targetAmount: 12000m);
        _targetRepository.GetByBranchAndMonthForUpdateAsync(
            command.BranchId, command.ReferenceYear, command.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        existing.TargetAmount.Should().Be(12000m);
        // Upsert: não deve criar um novo registro quando já existe meta para o período.
        await _targetRepository.DidNotReceive().AddAsync(Arg.Any<RevenueTarget>(), Arg.Any<CancellationToken>());
        // Commit explícito do handler (branch de atualização) + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
