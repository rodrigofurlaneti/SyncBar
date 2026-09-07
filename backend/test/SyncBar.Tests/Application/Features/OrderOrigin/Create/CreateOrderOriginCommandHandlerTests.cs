using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.Create;

public sealed class CreateOrderOriginCommandHandlerTests
{
    private static readonly DateTimeOffset FixedCurrentTime = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateOrderOriginCommandHandler _handler;

    public CreateOrderOriginCommandHandlerTests()
    {
        // TimeProvider.GetLocalNow() não é virtual — a implementação real chama GetUtcNow() e
        // LocalTimeZone (esses sim virtuais), então é isso que precisa ser stubado.
        _timeProvider.GetUtcNow().Returns(FixedCurrentTime);
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        _handler = new CreateOrderOriginCommandHandler(_orderOriginRepository, _timeProvider, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NameAlreadyExists_ShouldReturnFailureWithoutPersisting()
    {
        _orderOriginRepository.ExistsByNameAsync(1, 1, "LOCAL", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new CreateOrderOriginCommand(1, 1, "LOCAL"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderOrigin.AlreadyExists");
        await _orderOriginRepository.DidNotReceive().AddAsync(Arg.Any<SyncBar.Domain.Entities.OrderOrigin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureFromEntityCreate()
    {
        _orderOriginRepository.ExistsByNameAsync(null, null, "", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new CreateOrderOriginCommand(null, null, ""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderOrigin.EmptyName");
        await _orderOriginRepository.DidNotReceive().AddAsync(Arg.Any<SyncBar.Domain.Entities.OrderOrigin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndReturnNewId()
    {
        _orderOriginRepository.ExistsByNameAsync(null, null, "MARKETPLACE", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new CreateOrderOriginCommand(null, null, "Marketplace"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderOriginRepository.Received(1).AddAsync(
            Arg.Is<SyncBar.Domain.Entities.OrderOrigin>(o => o.Name == "MARKETPLACE"), Arg.Any<CancellationToken>());
        // 1 commit do save de negócio + 1 commit do log de auditoria gravado pelo BaseCommandHandler.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
