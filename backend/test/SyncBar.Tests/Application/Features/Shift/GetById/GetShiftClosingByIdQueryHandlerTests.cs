using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Shift.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.GetById;

public sealed class GetShiftClosingByIdQueryHandlerTests
{
    private readonly IShiftClosingRepository _shiftClosingRepository = Substitute.For<IShiftClosingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetShiftClosingByIdQueryHandler _handler;

    public GetShiftClosingByIdQueryHandlerTests()
    {
        _handler = new GetShiftClosingByIdQueryHandler(_shiftClosingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShiftClosingNotFound_ShouldReturnFailure()
    {
        _shiftClosingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ShiftClosing?)null);

        var result = await _handler.Handle(new GetShiftClosingByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotFound");
    }

    [Fact]
    public async Task Handle_ShiftClosingInactive_ShouldReturnFailure()
    {
        var shift = ShiftClosing.Open(1, 10).Value;
        shift.Deactivate();
        _shiftClosingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(shift);

        var result = await _handler.Handle(new GetShiftClosingByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotFound");
    }

    [Fact]
    public async Task Handle_ShiftClosingFound_ShouldReturnMappedResponse()
    {
        var shift = ShiftClosing.Open(1, 10).Value;
        _shiftClosingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(shift);

        var result = await _handler.Handle(new GetShiftClosingByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(shift.Id);
        result.Value.BranchId.Should().Be(1);
        result.Value.OpenedByEmployeeId.Should().Be(10);
        result.Value.ShiftClosingStatusId.Should().Be(shift.ShiftClosingStatusId);
    }
}
