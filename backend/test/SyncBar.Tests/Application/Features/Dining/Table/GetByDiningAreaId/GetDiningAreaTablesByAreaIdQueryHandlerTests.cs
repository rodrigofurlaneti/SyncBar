using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Table.GetByDiningAreaId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.GetByDiningAreaId;

public sealed class GetDiningAreaTablesByAreaIdQueryHandlerTests
{
    private readonly IDiningAreaTableRepository _repository = Substitute.For<IDiningAreaTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetDiningAreaTablesByAreaIdQueryHandler _handler;

    public GetDiningAreaTablesByAreaIdQueryHandlerTests()
    {
        _handler = new GetDiningAreaTablesByAreaIdQueryHandler(_repository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoTablesForArea_ShouldReturnEmptyCollection()
    {
        var query = new GetDiningAreaTablesByAreaIdQuery(1);
        _repository.GetByDiningAreaIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<DiningAreaTable>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler ainda grava log via finally, o que sempre comita.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AreaHasTables_ShouldReturnMappedListResponses()
    {
        var query = new GetDiningAreaTablesByAreaIdQuery(4);
        var table1 = DiningAreaTable.Create(4, 10).Value;
        var table2 = DiningAreaTable.Create(4, 11).Value;
        table2.Deactivate();
        _repository.GetByDiningAreaIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(new[] { table1, table2 });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().ContainSingle(r => r.DiningTableId == 10 && r.IsActive);
        result.Value.Should().ContainSingle(r => r.DiningTableId == 11 && !r.IsActive);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
