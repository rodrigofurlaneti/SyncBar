using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.Create;

public sealed class CreateKeetaIntegrationRefundDisputeCommandHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationRefundDisputeCommandHandler _handler;

    public CreateKeetaIntegrationRefundDisputeCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationRefundDisputeCommandHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_AfterSaleOrderIdAlreadyExists_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationRefundDisputeCommand(1, 2, "order-1", 500, 20m, "cliente pediu troca");
        _disputeRepository.ExistsByAfterSaleOrderIdAsync(500, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.AlreadyExists");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateKeetaIntegrationRefundDisputeCommand(1, 2, "order-1", 500, 20m, "cliente pediu troca");
        _disputeRepository.ExistsByAfterSaleOrderIdAsync(500, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AfterSaleOrderId.Should().Be(500);
        result.Value.ResolutionStatus.Should().Be("PENDING");
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
