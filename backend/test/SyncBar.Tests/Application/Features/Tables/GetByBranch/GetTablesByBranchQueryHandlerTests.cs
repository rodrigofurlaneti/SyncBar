using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Tables.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Tables.GetByBranch;

public sealed class GetTablesByBranchQueryHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetTablesByBranchQueryHandler _handler;

    public GetTablesByBranchQueryHandlerTests()
    {
        _handler = new GetTablesByBranchQueryHandler(_diningTableRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoTablesForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetTablesByBranchQuery(BranchId: 1);
        _diningTableRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DiningTable>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleTables_ShouldReturnOrderedByNumberAscending()
    {
        var query = new GetTablesByBranchQuery(BranchId: 1);
        var tableTen = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 10, capacity: 4).Value;
        var tableTwo = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 2, capacity: 2).Value;
        // Retorno do repositório propositalmente fora de ordem para provar o OrderBy do handler.
        _diningTableRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([tableTen, tableTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.Number).Should().ContainInOrder(2, 10);
    }

    [Fact]
    public async Task Handle_TableWithReadingValidationFlagsEnabled_ShouldMapAllFieldsToResponse()
    {
        var query = new GetTablesByBranchQuery(BranchId: 1);
        var table = DiningTable.Create(branchId: 1, tableStatusId: 2, number: 7, capacity: 6).Value;
        table.SetReadingValidationSettings(isCameraInputEnabled: true, isBarcodeEnabled: true, isQrCodeEnabled: false);
        _diningTableRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([table]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.BranchId.Should().Be(table.BranchId);
        response.TableStatusId.Should().Be(table.TableStatusId);
        response.Number.Should().Be(table.Number);
        response.Capacity.Should().Be(table.Capacity);
        response.IsCameraInputEnabled.Should().BeTrue();
        response.IsBarcodeEnabled.Should().BeTrue();
        response.IsQrCodeEnabled.Should().BeFalse();
    }
}
