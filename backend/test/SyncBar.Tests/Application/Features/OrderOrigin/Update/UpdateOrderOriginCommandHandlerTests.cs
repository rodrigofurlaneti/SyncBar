using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.Update;

public sealed class UpdateOrderOriginCommandHandlerTests
{
    private static readonly DateTimeOffset FixedCurrentTime = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateOrderOriginCommandHandler _handler;

    public UpdateOrderOriginCommandHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(FixedCurrentTime);
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        _handler = new UpdateOrderOriginCommandHandler(_orderOriginRepository, _timeProvider, _logRepository, _unitOfWork);
    }

    private static SyncBar.Domain.Entities.OrderOrigin CreateOrigin(string name = "LOCAL")
        => SyncBar.Domain.Entities.OrderOrigin.Create(1, 1, name, DateTime.Now).Value;

    [Fact]
    public async Task Handle_OrderOriginNotFound_ShouldReturnFailure()
    {
        _orderOriginRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((SyncBar.Domain.Entities.OrderOrigin?)null);

        var result = await _handler.Handle(new UpdateOrderOriginCommand(1, 1, 1, "Novo Nome", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderOrigin.NotFound");
    }

    [Fact]
    public async Task Handle_NameAlreadyUsedByAnotherOrigin_ShouldReturnFailure()
    {
        var origin = CreateOrigin();
        _orderOriginRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(origin);
        _orderOriginRepository.ExistsByNameAsync(1, 1, "WEBSITE", excludeId: 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new UpdateOrderOriginCommand(1, 1, 1, "WEBSITE", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderOrigin.AlreadyExists");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndCommit()
    {
        var origin = CreateOrigin();
        _orderOriginRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(origin);
        _orderOriginRepository.ExistsByNameAsync(1, 1, "WEBSITE", excludeId: 1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new UpdateOrderOriginCommand(1, 1, 1, "WEBSITE", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orderOriginRepository.Received(1).Update(origin);
        // 1 commit do save de negócio + 1 commit do log de auditoria gravado pelo BaseCommandHandler.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
