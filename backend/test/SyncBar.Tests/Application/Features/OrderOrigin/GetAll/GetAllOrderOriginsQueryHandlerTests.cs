using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.GetAll;

public sealed class GetAllOrderOriginsQueryHandlerTests
{
    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllOrderOriginsQueryHandler _handler;

    public GetAllOrderOriginsQueryHandlerTests()
    {
        _handler = new GetAllOrderOriginsQueryHandler(_orderOriginRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MultipleOrigins_ShouldReturnMappedList()
    {
        var origin = SyncBar.Domain.Entities.OrderOrigin.Create(null, null, "LOCAL", DateTime.Now).Value;
        _orderOriginRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([origin]);

        var result = await _handler.Handle(new GetAllOrderOriginsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Single().Name.Should().Be("LOCAL");
        result.Value.Single().Id.Should().Be(origin.Id);
    }

    [Fact]
    public async Task Handle_NoOrigins_ShouldReturnEmptyList()
    {
        _orderOriginRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllOrderOriginsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
