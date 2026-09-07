using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.GetFiltered;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.GetFiltered;

public sealed class GetFilteredOrderOriginsQueryHandlerTests
{
    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetFilteredOrderOriginsQueryHandler _handler;

    public GetFilteredOrderOriginsQueryHandlerTests()
    {
        _handler = new GetFilteredOrderOriginsQueryHandler(_orderOriginRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldForwardAllArgumentsAndReturnMappedList()
    {
        var origin = SyncBar.Domain.Entities.OrderOrigin.Create(1, 2, "CUSTOM", DateTime.Now).Value;
        _orderOriginRepository.GetFilteredAsync(1, 2, "cus", true, Arg.Any<CancellationToken>()).Returns([origin]);

        var result = await _handler.Handle(new GetFilteredOrderOriginsQuery(1, 2, "cus", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Name == "CUSTOM");
    }

    [Fact]
    public async Task Handle_NoFilters_ShouldPassNullsThroughAndReturnEmptyList()
    {
        _orderOriginRepository.GetFilteredAsync(null, null, null, null, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetFilteredOrderOriginsQuery(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
