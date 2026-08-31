using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.GetOpenSession;

public sealed class GetOpenSessionQueryHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOpenSessionQueryHandler _handler;

    public GetOpenSessionQueryHandlerTests()
    {
        _handler = new GetOpenSessionQueryHandler(_cashSessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoOpenSessionForCashRegister_ShouldReturnNotFoundFailure()
    {
        var query = new GetOpenSessionQuery(CashRegisterId: 7);
        _cashSessionRepository.GetOpenByCashRegisterAsync(query.CashRegisterId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpenSessionExists_ShouldMapAllFieldsToResponse()
    {
        var query = new GetOpenSessionQuery(CashRegisterId: 7);
        var session = CashSession.Open(cashRegisterId: query.CashRegisterId, openedByEmployeeId: 3, openingAmount: 150.75m).Value;

        _cashSessionRepository.GetOpenByCashRegisterAsync(query.CashRegisterId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Id.Should().Be(session.Id);
        response.CashRegisterId.Should().Be(session.CashRegisterId);
        response.CashSessionStatusId.Should().Be(session.CashSessionStatusId);
        response.OpenedByEmployeeId.Should().Be(session.OpenedByEmployeeId);
        response.OpeningAmount.Should().Be(session.OpeningAmount);
        response.OpenedAt.Should().Be(session.OpenedAt);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
