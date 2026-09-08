using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Shift.GetOpenShift;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.GetOpenShift;

public sealed class GetOpenShiftClosingQueryHandlerTests
{
    private readonly IShiftClosingRepository _shiftClosingRepository = Substitute.For<IShiftClosingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOpenShiftClosingQueryHandler _handler;

    public GetOpenShiftClosingQueryHandlerTests()
    {
        _handler = new GetOpenShiftClosingQueryHandler(_shiftClosingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoOpenShiftForBranch_ShouldReturnNotFoundFailure()
    {
        var query = new GetOpenShiftClosingQuery(BranchId: 7);
        _shiftClosingRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((ShiftClosing?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpenShiftExists_ShouldMapAllFieldsToResponse()
    {
        var query = new GetOpenShiftClosingQuery(BranchId: 7);
        var shift = ShiftClosing.Open(query.BranchId, openedByEmployeeId: 3).Value;

        _shiftClosingRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(shift);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Id.Should().Be(shift.Id);
        response.BranchId.Should().Be(shift.BranchId);
        response.ShiftClosingStatusId.Should().Be(shift.ShiftClosingStatusId);
        response.OpenedByEmployeeId.Should().Be(shift.OpenedByEmployeeId);
        response.PeriodStart.Should().Be(shift.PeriodStart);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
