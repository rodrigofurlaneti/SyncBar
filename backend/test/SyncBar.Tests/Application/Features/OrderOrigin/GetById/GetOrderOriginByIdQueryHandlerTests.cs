using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.GetById;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.GetById;

public sealed class GetOrderOriginByIdQueryHandlerTests
{
    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOrderOriginByIdQueryHandler _handler;

    public GetOrderOriginByIdQueryHandlerTests()
    {
        _handler = new GetOrderOriginByIdQueryHandler(_orderOriginRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrderOriginNotFound_ShouldReturnFailure()
    {
        _orderOriginRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((SyncBar.Domain.Entities.OrderOrigin?)null);

        var result = await _handler.Handle(new GetOrderOriginByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderOrigin.NotFound");
    }

    [Fact]
    public async Task Handle_OrderOriginFound_ShouldReturnMappedResponse()
    {
        var origin = SyncBar.Domain.Entities.OrderOrigin.Create(1, 1, "WEBSITE", DateTime.Now).Value;
        _orderOriginRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(origin);

        var result = await _handler.Handle(new GetOrderOriginByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("WEBSITE");
        result.Value.CompanyId.Should().Be(1);
    }
}
