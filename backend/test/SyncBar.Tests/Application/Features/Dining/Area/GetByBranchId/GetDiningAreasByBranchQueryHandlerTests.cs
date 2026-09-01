using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Area;
using SyncBar.Application.Features.Dining.Area.GetByBranchId;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningArea = SyncBar.Domain.Entities.DiningArea;

namespace SyncBar.Tests.Application.Features.Dining.Area.GetByBranchId;

public sealed class GetDiningAreasByBranchQueryHandlerTests
{
    private readonly IDiningAreaRepository _diningAreaRepository = Substitute.For<IDiningAreaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetDiningAreasByBranchQueryHandler _handler;

    public GetDiningAreasByBranchQueryHandlerTests()
    {
        _handler = new GetDiningAreasByBranchQueryHandler(_diningAreaRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoDiningAreasForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetDiningAreasByBranchQuery(BranchId: 1);
        _diningAreaRepository.GetByBranchIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleDiningAreas_ShouldMapActiveAndInactiveAreas()
    {
        var query = new GetDiningAreasByBranchQuery(BranchId: 1);
        var activeArea = DiningArea.Create(query.BranchId, "Salão Principal").Value;
        var inactiveArea = DiningArea.Create(query.BranchId, "Área Externa").Value;
        inactiveArea.Deactivate();

        _diningAreaRepository.GetByBranchIdAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([activeArea, inactiveArea]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().ContainSingle(r => r.Id == activeArea.Id && r.Name == activeArea.Name && r.IsActive);
        result.Value.Should().ContainSingle(r => r.Id == inactiveArea.Id && r.Name == inactiveArea.Name && !r.IsActive);
    }
}
