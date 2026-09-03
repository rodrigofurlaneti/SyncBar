using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Table.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.GetById;

public sealed class GetDiningAreaTableByIdQueryHandlerTests
{
    private readonly IDiningAreaTableRepository _repository = Substitute.For<IDiningAreaTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetDiningAreaTableByIdQueryHandler _handler;

    public GetDiningAreaTableByIdQueryHandlerTests()
    {
        _handler = new GetDiningAreaTableByIdQueryHandler(_repository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DiningAreaTableNotFound_ShouldReturnFailure()
    {
        var query = new GetDiningAreaTableByIdQuery(42);
        _repository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((DiningAreaTable?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiningAreaTableFound_ShouldReturnMappedResponse()
    {
        var query = new GetDiningAreaTableByIdQuery(8);
        var entity = DiningAreaTable.Create(6, 12).Value;
        _repository.GetByIdAsync(8, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(entity.Id);
        result.Value.DiningAreaId.Should().Be(6);
        result.Value.DiningTableId.Should().Be(12);
        result.Value.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
