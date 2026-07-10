using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.SetTarget;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class SetRevenueTargetCommandHandlerTests
{
    private readonly IRevenueTargetRepository _targetRepository = Substitute.For<IRevenueTargetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SetRevenueTargetCommandHandler CreateHandler()
        => new(_targetRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithoutExistingTarget_ShouldCreate()
    {
        _targetRepository.GetByBranchAndMonthForUpdateAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns((RevenueTarget?)null);

        var result = await CreateHandler().Handle(
            new SetRevenueTargetCommand(1, 2026, 7, 50000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _targetRepository.Received(1).AddAsync(
            Arg.Is<RevenueTarget>(t => t.TargetAmount == 50000m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingTarget_ShouldUpdateInsteadOfDuplicating()
    {
        var existing = RevenueTarget.Create(1, 2026, 7, 40000m).Value;
        _targetRepository.GetByBranchAndMonthForUpdateAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateHandler().Handle(
            new SetRevenueTargetCommand(1, 2026, 7, 55000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.TargetAmount.Should().Be(55000m);
        await _targetRepository.DidNotReceive().AddAsync(Arg.Any<RevenueTarget>(), Arg.Any<CancellationToken>());
    }
}
