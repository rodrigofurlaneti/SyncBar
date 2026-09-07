using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.OrderOrigin.GetByCompanyAndBranch;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.GetByCompanyAndBranch;

public sealed class GetOrderOriginsByCompanyAndBranchQueryHandlerTests
{
    private readonly IOrderOriginRepository _orderOriginRepository = Substitute.For<IOrderOriginRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOrderOriginsByCompanyAndBranchQueryHandler _handler;

    public GetOrderOriginsByCompanyAndBranchQueryHandlerTests()
    {
        _handler = new GetOrderOriginsByCompanyAndBranchQueryHandler(_orderOriginRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MatchingOrigins_ShouldReturnMappedList()
    {
        var origin = SyncBar.Domain.Entities.OrderOrigin.Create(1, 2, "CUSTOM", DateTime.Now).Value;
        _orderOriginRepository.GetByCompanyAndBranchAsync(1, 2, Arg.Any<CancellationToken>()).Returns([origin]);

        var result = await _handler.Handle(new GetOrderOriginsByCompanyAndBranchQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Name == "CUSTOM");
    }

    [Fact]
    public async Task Handle_NoMatchingOrigins_ShouldReturnEmptyList()
    {
        _orderOriginRepository.GetByCompanyAndBranchAsync(1, 2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetOrderOriginsByCompanyAndBranchQuery(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
