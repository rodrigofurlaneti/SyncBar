using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.ExistsByName;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.ExistsByName;

public sealed class ExistsOrderOriginByNameQueryHandlerTests
{
    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsOrderOriginByNameQueryHandler _handler;

    public ExistsOrderOriginByNameQueryHandlerTests()
    {
        _handler = new ExistsOrderOriginByNameQueryHandler(_orderOriginRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NameExists_ShouldReturnTrue()
    {
        _orderOriginRepository.ExistsByNameAsync(1, 1, "LOCAL", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsOrderOriginByNameQuery(1, 1, "LOCAL"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NameDoesNotExist_ShouldReturnFalse()
    {
        _orderOriginRepository.ExistsByNameAsync(1, 1, "LOCAL", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsOrderOriginByNameQuery(1, 1, "LOCAL"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExcludeId_ShouldForwardExcludeIdToRepository()
    {
        _orderOriginRepository.ExistsByNameAsync(1, 1, "LOCAL", excludeId: 5, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsOrderOriginByNameQuery(1, 1, "LOCAL", ExcludeId: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        await _orderOriginRepository.Received(1).ExistsByNameAsync(1, 1, "LOCAL", 5, Arg.Any<CancellationToken>());
    }
}
