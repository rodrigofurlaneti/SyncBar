using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Area;
using SyncBar.Application.Features.Dining.Area.GetById;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningArea = SyncBar.Domain.Entities.DiningArea;

namespace SyncBar.Tests.Application.Features.Dining.Area.GetById;

public sealed class GetDiningAreaByIdQueryHandlerTests
{
    private readonly IDiningAreaRepository _diningAreaRepository = Substitute.For<IDiningAreaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetDiningAreaByIdQueryHandler _handler;

    public GetDiningAreaByIdQueryHandlerTests()
    {
        _handler = new GetDiningAreaByIdQueryHandler(_diningAreaRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DiningAreaNotFound_ShouldReturnFailure()
    {
        var query = new GetDiningAreaByIdQuery(Id: 1);
        _diningAreaRepository.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns((DiningArea?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningArea.NotFound");
    }

    [Fact]
    public async Task Handle_DiningAreaFound_ShouldReturnItsData()
    {
        var query = new GetDiningAreaByIdQuery(Id: 1);
        var diningArea = DiningArea.Create(branchId: 1, name: "Salão Principal").Value;
        _diningAreaRepository.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns(diningArea);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(diningArea.Id);
        result.Value.Name.Should().Be(diningArea.Name);
        result.Value.IsActive.Should().Be(diningArea.IsActive);
    }
}
