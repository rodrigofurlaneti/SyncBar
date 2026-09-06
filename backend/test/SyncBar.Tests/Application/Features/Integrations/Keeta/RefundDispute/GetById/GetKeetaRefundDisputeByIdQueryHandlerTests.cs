using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetById;

public sealed class GetKeetaRefundDisputeByIdQueryHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaRefundDisputeByIdQueryHandler _handler;

    public GetKeetaRefundDisputeByIdQueryHandlerTests()
    {
        _handler = new GetKeetaRefundDisputeByIdQueryHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DisputeNotFound_ShouldReturnNotFound()
    {
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationRefundDispute?)null);

        var result = await _handler.Handle(new GetKeetaRefundDisputeByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_DisputeFound_ShouldReturnMappedResponse()
    {
        var dispute = KeetaIntegrationRefundDispute.Create(1, 2, "order-1", 500, 20m, "motivo").Value;
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(new GetKeetaRefundDisputeByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AfterSaleOrderId.Should().Be(500);
        result.Value.ResolutionStatus.Should().Be("PENDING");
    }
}
