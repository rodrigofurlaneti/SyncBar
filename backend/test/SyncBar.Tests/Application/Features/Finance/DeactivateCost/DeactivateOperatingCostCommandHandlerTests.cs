using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.DeactivateCost;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.DeactivateCost;

public sealed class DeactivateOperatingCostCommandHandlerTests
{
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateOperatingCostCommandHandler _handler;

    public DeactivateOperatingCostCommandHandlerTests()
    {
        _handler = new DeactivateOperatingCostCommandHandler(_costRepository, _logRepository, _unitOfWork);
    }

    private static OperatingCost CreateActiveCost()
        => OperatingCost.Create(
            branchId: 1, costTypeId: 1, description: "Aluguel", amount: 1500m,
            referenceYear: 2026, referenceMonth: 9).Value;

    [Fact]
    public async Task Handle_CostNotFound_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var command = new DeactivateOperatingCostCommand(OperatingCostId: 42);
        _costRepository.GetByIdForUpdateAsync(command.OperatingCostId, Arg.Any<CancellationToken>())
            .Returns((OperatingCost?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OperatingCost.NotFound");
        // Só o commit do finally da base — o handler não chama commit explícito nesse caminho.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CostAlreadyInactive_ShouldReturnNotFoundFailure()
    {
        var cost = CreateActiveCost();
        cost.Deactivate();
        var command = new DeactivateOperatingCostCommand(OperatingCostId: 42);
        _costRepository.GetByIdForUpdateAsync(command.OperatingCostId, Arg.Any<CancellationToken>())
            .Returns(cost);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OperatingCost.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveCost_ShouldDeactivateSameInstanceAndCommit()
    {
        var cost = CreateActiveCost();
        var command = new DeactivateOperatingCostCommand(OperatingCostId: 42);
        _costRepository.GetByIdForUpdateAsync(command.OperatingCostId, Arg.Any<CancellationToken>())
            .Returns(cost);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // O handler não chama nenhum método de update explícito no repositório — o
        // IOperatingCostRepository real nem expõe um: ele só muda o estado in-memory da
        // MESMA instância retornada por GetByIdForUpdateAsync e confia no rastreamento do
        // UnitOfWork/EF para persistir a mudança no Commit.
        cost.IsActive.Should().BeFalse();
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
